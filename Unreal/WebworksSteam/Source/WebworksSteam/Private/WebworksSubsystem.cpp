#include "WebworksSubsystem.h"

#include "Dom/JsonObject.h"
#include "Dom/JsonValue.h"
#include "Engine/GameInstance.h"
#include "Engine/World.h"
#include "GenericPlatform/GenericPlatformHttp.h"
#include "HAL/FileManager.h"
#include "HttpModule.h"
#include "Interfaces/IHttpRequest.h"
#include "Interfaces/IHttpResponse.h"
#include "Misc/FileHelper.h"
#include "Misc/Guid.h"
#include "Misc/Paths.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"
#include "TimerManager.h"
#include "UObject/UObjectGlobals.h"

namespace
{
	const TCHAR* IdentityFolder = TEXT("Webworks");
	const TCHAR* AccountFile = TEXT("account.txt");
	const TCHAR* DeviceFile = TEXT("device.txt");

	FString SerializeObject(const TSharedPtr<FJsonObject>& Body)
	{
		if (!Body.IsValid()) return TEXT("{}");
		FString Out;
		const TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Out);
		FJsonSerializer::Serialize(Body.ToSharedRef(), Writer);
		return Out;
	}

	TSharedPtr<FJsonObject> ParseObject(const FString& Text)
	{
		TSharedPtr<FJsonObject> Obj;
		const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Text);
		if (FJsonSerializer::Deserialize(Reader, Obj) && Obj.IsValid()) return Obj;
		return nullptr;
	}

	FString Field(const TSharedPtr<FJsonObject>& Obj, const TCHAR* Name)
	{
		FString Value;
		if (Obj.IsValid()) Obj->TryGetStringField(Name, Value);
		return Value;
	}

	TSharedPtr<FJsonObject> Child(const TSharedPtr<FJsonObject>& Obj, const TCHAR* Name)
	{
		const TSharedPtr<FJsonObject>* Found = nullptr;
		if (Obj.IsValid() && Obj->TryGetObjectField(Name, Found) && Found) return *Found;
		return nullptr;
	}

	TArray<TSharedPtr<FJsonValue>> ArrayField(const TSharedPtr<FJsonObject>& Obj, const TCHAR* Name)
	{
		const TArray<TSharedPtr<FJsonValue>>* Found = nullptr;
		if (Obj.IsValid() && Obj->TryGetArrayField(Name, Found) && Found) return *Found;
		return TArray<TSharedPtr<FJsonValue>>();
	}

	int32 IntField(const TSharedPtr<FJsonObject>& Obj, const TCHAR* Name)
	{
		int32 Value = 0;
		if (Obj.IsValid()) Obj->TryGetNumberField(Name, Value);
		return Value;
	}

	bool BoolField(const TSharedPtr<FJsonObject>& Obj, const TCHAR* Name)
	{
		bool Value = false;
		if (Obj.IsValid()) Obj->TryGetBoolField(Name, Value);
		return Value;
	}

	FString ErrorText(bool bOk, const TSharedPtr<FJsonObject>& Json, const FHttpResponsePtr& Response)
	{
		if (Json.IsValid())
		{
			const FString Code = Field(Json, TEXT("error"));
			if (!Code.IsEmpty()) return Code;
		}
		if (Response.IsValid()) return FString::Printf(TEXT("http_%d"), Response->GetResponseCode());
		return bOk ? TEXT("") : TEXT("server_unreachable");
	}
}

void UWebworksSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	Settings = GetMutableDefault<UWebworksSettings>();
	LoadPersistedIdentity();
	RestoreSavedSession();
}

void UWebworksSubsystem::Deinitialize()
{
	if (UWorld* World = GetWorld()) World->GetTimerManager().ClearTimer(PresenceTimer);
	Settings = nullptr;
	Super::Deinitialize();
}

FString UWebworksSubsystem::GetServerUrl() const
{
	FString Url = Settings ? Settings->ServerUrl : TEXT("http://localhost:3000");
	Url.TrimStartAndEndInline();
	if (Url.IsEmpty()) Url = TEXT("http://localhost:3000");

	// Configs copied from older projects sometimes paste a fully-qualified API base into ServerUrl
	// (e.g. http://host:3000/api/Unity/Facepunch). Drop the suffix so ApiUrl never doubles /api.
	const int32 ApiIndex = Url.Find(TEXT("/api/"), ESearchCase::IgnoreCase);
	if (ApiIndex > 0)
	{
		Url = Url.Left(ApiIndex);
	}
	else if (Url.EndsWith(TEXT("/api"), ESearchCase::IgnoreCase))
	{
		Url = Url.LeftChop(4);
	}

	while (Url.EndsWith(TEXT("/"))) Url.LeftChopInline(1);
	return Url;
}

FString UWebworksSubsystem::ApiUrl(const FString& Path) const
{
	FString Engine = Settings ? Settings->EnginePath : TEXT("Unreal/Steam");
	Engine.TrimStartAndEndInline();
	if (Engine.StartsWith(TEXT("api/"), ESearchCase::IgnoreCase)) Engine.RightChopInline(4);
	while (Engine.StartsWith(TEXT("/"))) Engine.RightChopInline(1);
	while (Engine.EndsWith(TEXT("/"))) Engine.LeftChopInline(1);

	// An empty EnginePath means the wrapper-less /api mount. Any historical prefix works too:
	// the server mounts the same handlers under Unity/Facepunch, Unity/SteamworksNET, Weeam, ...
	const FString Base = GetServerUrl() + TEXT("/api");
	return Engine.IsEmpty() ? (Base + Path) : (Base + TEXT("/") + Engine + Path);
}

FString UWebworksSubsystem::DeviceKey()
{
	const FString File = FPaths::Combine(FPaths::ProjectSavedDir(), IdentityFolder, DeviceFile);
	FString Key;
	if (FFileHelper::LoadFileToString(Key, *File))
	{
		Key.TrimStartAndEndInline();
		if (Key.Len() >= 16) return Key;
	}

	Key = FGuid::NewGuid().ToString(EGuidFormats::Digits).ToLower();
	FFileHelper::SaveStringToFile(Key, *File);
	return Key;
}

void UWebworksSubsystem::LoadPersistedIdentity()
{
	if (!Settings || !Settings->bPersistAccountToken) return;
	const FString File = FPaths::Combine(FPaths::ProjectSavedDir(), IdentityFolder, AccountFile);
	FString Token;
	if (FFileHelper::LoadFileToString(Token, *File))
	{
		Token.TrimStartAndEndInline();
		AccountToken = Token;
	}
}

void UWebworksSubsystem::SaveAccountToken()
{
	if (!Settings || !Settings->bPersistAccountToken || AccountToken.IsEmpty()) return;
	const FString File = FPaths::Combine(FPaths::ProjectSavedDir(), IdentityFolder, AccountFile);
	FFileHelper::SaveStringToFile(AccountToken, *File);
}

void UWebworksSubsystem::ClearSavedAccountToken()
{
	const FString File = FPaths::Combine(FPaths::ProjectSavedDir(), IdentityFolder, AccountFile);
	IFileManager::Get().Delete(*File, false, true, true);
}

void UWebworksSubsystem::HandleJson(const FString& Verb, const FString& Path, const TSharedPtr<FJsonObject>& Body, TFunction<void(bool, const TSharedPtr<FJsonObject>&)> OnDone)
{
	const TSharedRef<IHttpRequest> Request = FHttpModule::Get().CreateRequest();
	Request->SetURL(ApiUrl(Path));
	Request->SetVerb(Verb);
	Request->SetHeader(TEXT("Content-Type"), TEXT("application/json"));
	if (!AccountToken.IsEmpty())
	{
		Request->SetHeader(TEXT("x-webworks-account"), AccountToken);
		Request->SetHeader(TEXT("x-webeam-account"), AccountToken); // pre-rename servers only know this name
	}
	if (!SessionToken.IsEmpty())
	{
		Request->SetHeader(TEXT("x-webworks-session"), SessionToken);
		Request->SetHeader(TEXT("x-webeam-session"), SessionToken);
	}
	if (Verb != TEXT("GET")) Request->SetContentAsString(SerializeObject(Body));

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	Request->OnProcessRequestComplete().BindLambda([WeakThis, OnDone](FHttpRequestPtr, FHttpResponsePtr Response, bool bOk)
	{
		if (!WeakThis.IsValid()) return;
		const bool bSuccess = bOk && Response.IsValid() && Response->GetResponseCode() >= 200 && Response->GetResponseCode() < 300;
		const TSharedPtr<FJsonObject> Json = Response.IsValid() ? ParseObject(Response->GetContentAsString()) : nullptr;
		OnDone(bSuccess, Json);
	});
	Request->ProcessRequest();
}

void UWebworksSubsystem::ApplyAccount(const TSharedPtr<FJsonObject>& AccountObj)
{
	if (!AccountObj.IsValid()) return;
	Account.Username = Field(AccountObj, TEXT("username"));
	Account.DisplayName = Field(AccountObj, TEXT("displayName"));
	Account.SteamId = Field(AccountObj, TEXT("steamId"));
	Account.bHasAvatar = BoolField(AccountObj, TEXT("hasAvatar"));
}

void UWebworksSubsystem::RestoreSavedSession()
{
	if (AccountToken.IsEmpty()) return;

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("GET"), TEXT("/Accounts/Me"), nullptr, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		const TSharedPtr<FJsonObject> AccountObj = Child(Json, TEXT("account"));
		if (!bOk || !AccountObj.IsValid())
		{
			Self->AccountToken.Empty();
			Self->ClearSavedAccountToken();
			Self->EnsureSession();
			return;
		}
		Self->ApplyAccount(AccountObj);
		Self->bSessionActive = true;
		Self->OnLoginComplete.Broadcast(true, TEXT(""));
		Self->EnsureSession();
		Self->GetFriends();
	});
}

void UWebworksSubsystem::EnsureSession()
{
	if (SessionToken.Len() > 0) return;

	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("appId"), Settings ? Settings->AppId : TEXT("0"));
	Body->SetStringField(TEXT("deviceKey"), DeviceKey());
	if (!Account.DisplayName.IsEmpty()) Body->SetStringField(TEXT("name"), Account.DisplayName);
	if (!AccountToken.IsEmpty()) Body->SetStringField(TEXT("accountToken"), AccountToken);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/User/GuestSession"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnPresenceChanged.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}

		Self->SessionToken = Field(Json, TEXT("sessionToken"));
		Self->Account.SteamId = Field(Json, TEXT("steamId"));
		const FString Name = Field(Json, TEXT("name"));
		if (!Name.IsEmpty()) Self->Account.DisplayName = Name;
		Self->Account.bHasAvatar = BoolField(Json, TEXT("hasAvatar"));
		Self->bSessionActive = true;
		Self->OnPresenceChanged.Broadcast(true, TEXT(""));

		if (UWorld* World = Self->GetWorld())
		{
			World->GetTimerManager().SetTimer(Self->PresenceTimer, Self, &UWebworksSubsystem::RefreshProfile, 60.0f, true);
		}
	});
}

void UWebworksSubsystem::RefreshProfile()
{
	if (SessionToken.IsEmpty()) return;
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("GET"), TEXT("/User/Profile"), nullptr, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid() || !bOk || !Json.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		const FString SteamId = Field(Json, TEXT("steamId"));
		const FString Name = Field(Json, TEXT("name"));
		const FString Username = Field(Json, TEXT("username"));
		if (!SteamId.IsEmpty()) Self->Account.SteamId = SteamId;
		if (!Name.IsEmpty()) Self->Account.DisplayName = Name;
		if (!Username.IsEmpty()) Self->Account.Username = Username;
		Self->Account.bHasAvatar = BoolField(Json, TEXT("hasAvatar"));
		Self->bSessionActive = true;
	});
}

void UWebworksSubsystem::LoginUser(const FString& Username, const FString& Password)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	Body->SetStringField(TEXT("password"), Password);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Accounts/Login"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnLoginComplete.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}

		Self->AccountToken = Field(Json, TEXT("token"));
		Self->ApplyAccount(Child(Json, TEXT("account")));
		Self->bSessionActive = true;
		Self->SaveAccountToken();
		Self->SessionToken.Empty();
		Self->OnLoginComplete.Broadcast(true, TEXT(""));
		Self->EnsureSession();
		Self->GetFriends();
	});
}

void UWebworksSubsystem::RegisterUser(const FString& Username, const FString& Password, const FString& DisplayName, const FString& AvatarBase64)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	Body->SetStringField(TEXT("password"), Password);
	if (!DisplayName.IsEmpty()) Body->SetStringField(TEXT("displayName"), DisplayName);
	if (!AvatarBase64.IsEmpty()) Body->SetStringField(TEXT("avatarBase64"), AvatarBase64);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Accounts/Register"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnRegisterComplete.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}

		Self->AccountToken = Field(Json, TEXT("token"));
		Self->ApplyAccount(Child(Json, TEXT("account")));
		Self->bSessionActive = true;
		Self->SaveAccountToken();
		Self->SessionToken.Empty();
		Self->OnRegisterComplete.Broadcast(true, TEXT(""));
		Self->EnsureSession();
	});
}

void UWebworksSubsystem::LoginAsGuest(const FString& DisplayName)
{
	AccountToken.Empty();
	ClearSavedAccountToken();
	SessionToken.Empty();
	Account = FWebworksAccount();
	Account.DisplayName = DisplayName;
	CurrentSessionId.Empty();
	Friends.Empty();

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	const FString GuestName = DisplayName;
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("appId"), Settings ? Settings->AppId : TEXT("0"));
	Body->SetStringField(TEXT("deviceKey"), DeviceKey());
	if (!GuestName.IsEmpty()) Body->SetStringField(TEXT("name"), GuestName);

	HandleJson(TEXT("POST"), TEXT("/User/GuestSession"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnLoginComplete.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}
		Self->SessionToken = Field(Json, TEXT("sessionToken"));
		Self->Account.SteamId = Field(Json, TEXT("steamId"));
		Self->Account.DisplayName = Field(Json, TEXT("name"));
		Self->Account.Username.Empty();
		Self->Account.bHasAvatar = BoolField(Json, TEXT("hasAvatar"));
		Self->bSessionActive = true;
		Self->OnLoginComplete.Broadcast(true, TEXT(""));
		if (UWorld* World = Self->GetWorld())
		{
			World->GetTimerManager().SetTimer(Self->PresenceTimer, Self, &UWebworksSubsystem::RefreshProfile, 60.0f, true);
		}
	});
}

void UWebworksSubsystem::LinkAccount(const FString& Username, const FString& Password)
{
	const TSharedPtr<FJsonObject> LoginBody = MakeShared<FJsonObject>();
	LoginBody->SetStringField(TEXT("username"), Username);
	LoginBody->SetStringField(TEXT("password"), Password);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Accounts/Login"), LoginBody, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnLoginComplete.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}

		Self->AccountToken = Field(Json, TEXT("token"));
		Self->ApplyAccount(Child(Json, TEXT("account")));
		Self->SaveAccountToken();

		const TSharedPtr<FJsonObject> LinkBody = MakeShared<FJsonObject>();
		LinkBody->SetStringField(TEXT("accountToken"), Self->AccountToken);

		HandleJson(TEXT("POST"), TEXT("/User/LinkAccount"), LinkBody, [WeakThis](bool bLinkOk, const TSharedPtr<FJsonObject>& LinkJson)
		{
			if (!WeakThis.IsValid()) return;
			UWebworksSubsystem* Self = WeakThis.Get();
			if (!bLinkOk || !LinkJson.IsValid())
			{
				Self->OnLoginComplete.Broadcast(false, LinkJson.IsValid() ? Field(LinkJson, TEXT("error")) : TEXT("link_failed"));
				return;
			}
			const FString SteamId = Field(LinkJson, TEXT("steamId"));
			const FString Name = Field(LinkJson, TEXT("name"));
			if (!SteamId.IsEmpty()) Self->Account.SteamId = SteamId;
			if (!Name.IsEmpty()) Self->Account.DisplayName = Name;
			Self->bSessionActive = true;
			Self->OnLoginComplete.Broadcast(true, TEXT(""));
			Self->GetFriends();
		});
	});
}

void UWebworksSubsystem::LogoutUser()
{
	if (!AccountToken.IsEmpty()) HandleJson(TEXT("POST"), TEXT("/Accounts/Logout"), nullptr, [](bool, const TSharedPtr<FJsonObject>&) {});

	AccountToken.Empty();
	SessionToken.Empty();
	CurrentSessionId.Empty();
	LastCloudFile.Empty();
	LastCloudData.Empty();
	Account = FWebworksAccount();
	Friends.Empty();
	bSessionActive = false;
	ClearSavedAccountToken();
	if (UWorld* World = GetWorld()) World->GetTimerManager().ClearTimer(PresenceTimer);
	OnLoginComplete.Broadcast(true, TEXT("logged_out"));
	EnsureSession();
}

void UWebworksSubsystem::UpdateAccountIcon(const FString& AvatarBase64)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("avatarBase64"), AvatarBase64);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("PATCH"), TEXT("/Accounts/Me"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk)
		{
			Self->OnAccountUpdated.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}
		const TSharedPtr<FJsonObject> AccountObj = Child(Json, TEXT("account"));
		if (AccountObj.IsValid()) Self->ApplyAccount(AccountObj);
		Self->OnAccountUpdated.Broadcast(true, TEXT(""));
		Self->GetFriends();
	});
}

void UWebworksSubsystem::GetFriends()
{
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("GET"), TEXT("/Friends/Graph"), nullptr, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnFriendsUpdated.Broadcast(Self->GetCachedFriends());
			return;
		}

		Self->Friends.Empty();
		for (const TSharedPtr<FJsonValue>& Value : ArrayField(Json, TEXT("accepted")))
		{
			const TSharedPtr<FJsonObject> Entry = Value.IsValid() ? Value->AsObject() : nullptr;
			if (!Entry.IsValid()) continue;
			FWebworksFriend Friend;
			Friend.Username = Field(Entry, TEXT("username"));
			Friend.DisplayName = Field(Entry, TEXT("displayName"));
			Friend.SteamId = Field(Entry, TEXT("steamId"));
			Friend.AppId = Field(Entry, TEXT("game"));
			Friend.bOnline = Field(Entry, TEXT("personaState")) == TEXT("Online");
			Self->Friends.Add(Friend);
		}
		Self->OnFriendsUpdated.Broadcast(Self->GetCachedFriends());
	});
}

FWebworksFriendList UWebworksSubsystem::GetCachedFriends() const
{
	FWebworksFriendList List;
	List.Friends = Friends;
	return List;
}

void UWebworksSubsystem::SendFriendInvite(const FString& Username)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Friends/Add"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnAccountUpdated.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
		if (bOk) WeakThis->GetFriends();
	});
}

void UWebworksSubsystem::AcceptFriendInvite(const FString& Username)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Friends/Accept"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnAccountUpdated.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
		if (bOk) WeakThis->GetFriends();
	});
}

void UWebworksSubsystem::RemoveFriend(const FString& Username)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Friends/Remove"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnAccountUpdated.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
		if (bOk) WeakThis->GetFriends();
	});
}

void UWebworksSubsystem::BlockFriend(const FString& Username)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Friends/Block"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnAccountUpdated.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
		if (bOk) WeakThis->GetFriends();
	});
}

void UWebworksSubsystem::UnblockFriend(const FString& Username)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("username"), Username);
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Friends/Unblock"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnAccountUpdated.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
		if (bOk) WeakThis->GetFriends();
	});
}

void UWebworksSubsystem::SetRichPresence(const FString& Key, const FString& Value)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("key"), Key);
	Body->SetStringField(TEXT("value"), Value);
	HandleJson(TEXT("POST"), TEXT("/Friends/RichPresence"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::ClearRichPresence()
{
	HandleJson(TEXT("DELETE"), TEXT("/Friends/RichPresence"), nullptr, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::CreateSession(const FString& SessionName, int32 MaxMembers, bool bPublic)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetNumberField(TEXT("maxMembers"), FMath::Max(1, MaxMembers));

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Matchmaking/CreateLobby"), Body, [WeakThis, SessionName, bPublic](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnSessionJoined.Broadcast(FWebworksSessionInfo());
			return;
		}

		FWebworksSessionInfo Info;
		Info.LobbyId = Field(Json, TEXT("id"));
		Info.OwnerId = Field(Json, TEXT("ownerId"));
		Info.MaxMembers = IntField(Json, TEXT("maxMembers"));
		Info.MemberCount = IntField(Json, TEXT("memberCount"));
		Info.Visibility = Field(Json, TEXT("visibility"));
		Self->CurrentSessionId = Info.LobbyId;

		if (!SessionName.IsEmpty())
		{
			const TSharedPtr<FJsonObject> Data = MakeShared<FJsonObject>();
			Data->SetStringField(TEXT("lobbyId"), Info.LobbyId);
			Data->SetStringField(TEXT("key"), TEXT("name"));
			Data->SetStringField(TEXT("value"), SessionName);
			Self->HandleJson(TEXT("POST"), TEXT("/Matchmaking/LobbyData"), Data, [](bool, const TSharedPtr<FJsonObject>&) {});
		}
		if (!bPublic)
		{
			const TSharedPtr<FJsonObject> Visibility = MakeShared<FJsonObject>();
			Visibility->SetStringField(TEXT("lobbyId"), Info.LobbyId);
			Visibility->SetStringField(TEXT("mode"), TEXT("private"));
			Self->HandleJson(TEXT("POST"), TEXT("/Matchmaking/LobbyVisibility"), Visibility, [](bool, const TSharedPtr<FJsonObject>&) {});
		}

		Self->OnSessionJoined.Broadcast(Info);
	});
}

void UWebworksSubsystem::FindSessions(int32 MaxResults)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetNumberField(TEXT("max"), FMath::Max(1, MaxResults));

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Matchmaking/LobbyList"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		FWebworksSessionList List;
		if (bOk && Json.IsValid())
		{
			for (const TSharedPtr<FJsonValue>& Value : ArrayField(Json, TEXT("lobbies")))
			{
				const TSharedPtr<FJsonObject> Entry = Value.IsValid() ? Value->AsObject() : nullptr;
				if (!Entry.IsValid()) continue;
				FWebworksSessionInfo Info;
				Info.LobbyId = Field(Entry, TEXT("id"));
				Info.OwnerId = Field(Entry, TEXT("ownerId"));
				Info.MaxMembers = IntField(Entry, TEXT("maxMembers"));
				Info.MemberCount = IntField(Entry, TEXT("memberCount"));
				Info.Visibility = Field(Entry, TEXT("visibility"));

				const TSharedPtr<FJsonObject> Data = Child(Entry, TEXT("data"));
				if (Data.IsValid())
				{
					for (const auto& Pair : Data->Values)
					{
						Info.Data.Add(Pair.Key, Pair.Value.IsValid() ? Pair.Value->AsString() : TEXT(""));
					}
				}
				List.Sessions.Add(Info);
			}
		}
		Self->OnSessionsFound.Broadcast(List);
	});
}

void UWebworksSubsystem::JoinSession(const FString& LobbyId)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("lobbyId"), LobbyId);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/Matchmaking/JoinLobby"), Body, [WeakThis, LobbyId](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnSessionJoined.Broadcast(FWebworksSessionInfo());
			return;
		}
		FWebworksSessionInfo Info;
		Info.LobbyId = Field(Json, TEXT("id"));
		Info.OwnerId = Field(Json, TEXT("ownerId"));
		Info.MaxMembers = IntField(Json, TEXT("maxMembers"));
		Info.MemberCount = IntField(Json, TEXT("memberCount"));
		Info.Visibility = Field(Json, TEXT("visibility"));
		Self->CurrentSessionId = Info.LobbyId.IsEmpty() ? LobbyId : Info.LobbyId;
		Self->OnSessionJoined.Broadcast(Info);
	});
}

void UWebworksSubsystem::LeaveSession()
{
	if (CurrentSessionId.IsEmpty()) return;
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("lobbyId"), CurrentSessionId);
	HandleJson(TEXT("POST"), TEXT("/Matchmaking/LeaveLobby"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
	CurrentSessionId.Empty();
	OnSessionJoined.Broadcast(FWebworksSessionInfo());
}

void UWebworksSubsystem::SetSessionData(const FString& Key, const FString& Value)
{
	if (CurrentSessionId.IsEmpty()) return;
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("lobbyId"), CurrentSessionId);
	Body->SetStringField(TEXT("key"), Key);
	Body->SetStringField(TEXT("value"), Value);
	HandleJson(TEXT("POST"), TEXT("/Matchmaking/LobbyData"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::SetSessionJoinable(bool bJoinable)
{
	if (CurrentSessionId.IsEmpty()) return;
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("lobbyId"), CurrentSessionId);
	Body->SetBoolField(TEXT("joinable"), bJoinable);
	HandleJson(TEXT("POST"), TEXT("/Matchmaking/LobbyJoinable"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::SetStatInt(const FString& Name, int32 Value)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("name"), Name);
	Body->SetNumberField(TEXT("value"), Value);
	HandleJson(TEXT("POST"), TEXT("/UserStats/Stat"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::SetAchievement(const FString& Name)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	const TArray<TSharedPtr<FJsonValue>> Unlocked = { MakeShared<FJsonValueString>(Name) };
	Body->SetArrayField(TEXT("unlocked"), Unlocked);
	HandleJson(TEXT("POST"), TEXT("/UserStats/Stats"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::StoreStats()
{
	HandleJson(TEXT("GET"), TEXT("/UserStats/Stats"), nullptr, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::UploadLeaderboardScore(const FString& Board, int32 Score)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("name"), Board);
	Body->SetNumberField(TEXT("score"), Score);
	HandleJson(TEXT("POST"), TEXT("/UserStats/Leaderboard/Submit"), Body, [](bool, const TSharedPtr<FJsonObject>&) {});
}

void UWebworksSubsystem::RequestLeaderboard(const FString& Board, int32 Count)
{
	const FString Query = TEXT("/UserStats/Leaderboard/Entries?name=") + FGenericPlatformHttp::UrlEncode(Board)
		+ TEXT("&type=global&offset=0&count=") + FString::FromInt(FMath::Clamp(Count, 1, 100));

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("GET"), Query, nullptr, [WeakThis, Board](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		FWebworksLeaderboard Result;
		Result.Name = Board;
		Result.bFound = bOk && Json.IsValid() && BoolField(Json, TEXT("found"));

		if (Result.bFound)
		{
			for (const TSharedPtr<FJsonValue>& Value : ArrayField(Json, TEXT("entries")))
			{
				const TSharedPtr<FJsonObject> Entry = Value.IsValid() ? Value->AsObject() : nullptr;
				if (!Entry.IsValid()) continue;
				FWebworksLeaderboardEntry Row;
				Row.SteamId = Field(Entry, TEXT("steamId"));
				Row.Name = Field(Entry, TEXT("name"));
				Row.Rank = IntField(Entry, TEXT("globalRank")) || IntField(Entry, TEXT("rank"));
				Row.Score = IntField(Entry, TEXT("score"));
				Result.Entries.Add(Row);
			}
		}
		Self->OnLeaderboardReceived.Broadcast(Result);
	});
}

void UWebworksSubsystem::CloudWrite(const FString& FileName, const FString& Base64Data)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("name"), FileName);
	Body->SetStringField(TEXT("data"), Base64Data);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/RemoteStorage/Write"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnCloudComplete.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
	});
}

void UWebworksSubsystem::CloudRead(const FString& FileName)
{
	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("GET"), TEXT("/RemoteStorage/Files"), nullptr, [WeakThis, FileName](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		UWebworksSubsystem* Self = WeakThis.Get();
		if (!bOk || !Json.IsValid())
		{
			Self->OnCloudComplete.Broadcast(false, Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable"));
			return;
		}

		for (const TSharedPtr<FJsonValue>& Value : ArrayField(Json, TEXT("files")))
		{
			const TSharedPtr<FJsonObject> Entry = Value.IsValid() ? Value->AsObject() : nullptr;
			if (!Entry.IsValid() || Field(Entry, TEXT("name")) != FileName) continue;
			Self->LastCloudFile = FileName;
			Self->LastCloudData = Field(Entry, TEXT("data"));
			Self->OnCloudComplete.Broadcast(true, TEXT(""));
			return;
		}
		Self->OnCloudComplete.Broadcast(false, TEXT("file_not_found"));
	});
}

void UWebworksSubsystem::CloudDelete(const FString& FileName)
{
	const TSharedPtr<FJsonObject> Body = MakeShared<FJsonObject>();
	Body->SetStringField(TEXT("name"), FileName);

	TWeakObjectPtr<UWebworksSubsystem> WeakThis(this);
	HandleJson(TEXT("POST"), TEXT("/RemoteStorage/Delete"), Body, [WeakThis](bool bOk, const TSharedPtr<FJsonObject>& Json)
	{
		if (!WeakThis.IsValid()) return;
		WeakThis->OnCloudComplete.Broadcast(bOk, bOk ? TEXT("") : (Json.IsValid() ? Field(Json, TEXT("error")) : TEXT("server_unreachable")));
	});
}

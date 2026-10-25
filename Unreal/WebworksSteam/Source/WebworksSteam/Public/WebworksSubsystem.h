#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "WebworksTypes.h"
#include "WebworksSubsystem.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FWebworksResultDelegate, bool, bSuccess, const FString&, Error);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FWebworksFriendsDelegate, const FWebworksFriendList&, FriendList);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FWebworksSessionsDelegate, const FWebworksSessionList&, SessionList);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FWebworksSessionDelegate, const FWebworksSessionInfo&, Session);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FWebworksLeaderboardDelegate, const FWebworksLeaderboard&, Leaderboard);

UCLASS(Config = Game, DefaultConfig)
class WEBWORKSSTEAM_API UWebworksSettings : public UObject
{
	GENERATED_BODY()

public:
	UPROPERTY(EditAnywhere, Config, Category = "Webworks") FString ServerUrl = TEXT("http://localhost:3000");
	UPROPERTY(EditAnywhere, Config, Category = "Webworks") FString EnginePath = TEXT("Unreal/Steam");
	UPROPERTY(EditAnywhere, Config, Category = "Webworks") FString AppId = TEXT("0");
	UPROPERTY(EditAnywhere, Config, Category = "Webworks") bool bPersistAccountToken = true;
};

UCLASS()
class WEBWORKSSTEAM_API UWebworksSubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksResultDelegate OnLoginComplete;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksResultDelegate OnRegisterComplete;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksResultDelegate OnAccountUpdated;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksFriendsDelegate OnFriendsUpdated;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksSessionsDelegate OnSessionsFound;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksSessionDelegate OnSessionJoined;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksResultDelegate OnPresenceChanged;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksResultDelegate OnCloudComplete;
	UPROPERTY(BlueprintAssignable, Category = "Webworks") FWebworksLeaderboardDelegate OnLeaderboardReceived;

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Login User"))
	void LoginUser(const FString& Username, const FString& Password);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Register User"))
	void RegisterUser(const FString& Username, const FString& Password, const FString& DisplayName, const FString& AvatarBase64);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Login As Guest"))
	void LoginAsGuest(const FString& DisplayName);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Logout User"))
	void LogoutUser();

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Update Account Icon"))
	void UpdateAccountIcon(const FString& AvatarBase64);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Link Current Session To Account"))
	void LinkAccount(const FString& Username, const FString& Password);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Account", meta = (DisplayName = "Restore Saved Session"))
	void RestoreSavedSession();

	UFUNCTION(BlueprintPure, Category = "Webworks|Account") bool IsLoggedIn() const { return bSessionActive; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") bool IsAccountLinked() const { return !Account.Username.IsEmpty(); }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") FString GetSteamId() const { return Account.SteamId; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") FString GetDisplayName() const { return Account.DisplayName; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") FWebworksAccount GetAccount() const { return Account; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") FString GetAccountToken() const { return AccountToken; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") FString GetSessionToken() const { return SessionToken; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Account") FString GetServerUrl() const;

	UFUNCTION(BlueprintCallable, Category = "Webworks|Friends", meta = (DisplayName = "Get Friends"))
	void GetFriends();
	UFUNCTION(BlueprintCallable, Category = "Webworks|Friends") void SendFriendInvite(const FString& Username);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Friends") void AcceptFriendInvite(const FString& Username);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Friends") void RemoveFriend(const FString& Username);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Friends") void BlockFriend(const FString& Username);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Friends") void UnblockFriend(const FString& Username);
	UFUNCTION(BlueprintPure, Category = "Webworks|Friends") FWebworksFriendList GetCachedFriends() const;

	UFUNCTION(BlueprintCallable, Category = "Webworks|Presence") void SetRichPresence(const FString& Key, const FString& Value);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Presence") void ClearRichPresence();

	UFUNCTION(BlueprintCallable, Category = "Webworks|Sessions", meta = (DisplayName = "Create Session"))
	void CreateSession(const FString& SessionName, int32 MaxMembers, bool bPublic);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Sessions", meta = (DisplayName = "Find Sessions"))
	void FindSessions(int32 MaxResults);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Sessions", meta = (DisplayName = "Join Session"))
	void JoinSession(const FString& LobbyId);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Sessions", meta = (DisplayName = "Leave Session"))
	void LeaveSession();

	UFUNCTION(BlueprintCallable, Category = "Webworks|Sessions") void SetSessionData(const FString& Key, const FString& Value);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Sessions") void SetSessionJoinable(bool bJoinable);
	UFUNCTION(BlueprintPure, Category = "Webworks|Sessions") FString GetCurrentSessionId() const { return CurrentSessionId; }

	UFUNCTION(BlueprintCallable, Category = "Webworks|Stats") void SetStatInt(const FString& Name, int32 Value);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Stats") void SetAchievement(const FString& Name);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Stats") void StoreStats();
	UFUNCTION(BlueprintCallable, Category = "Webworks|Stats") void UploadLeaderboardScore(const FString& Board, int32 Score);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Stats") void RequestLeaderboard(const FString& Board, int32 Count);

	UFUNCTION(BlueprintCallable, Category = "Webworks|Cloud") void CloudWrite(const FString& FileName, const FString& Base64Data);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Cloud") void CloudRead(const FString& FileName);
	UFUNCTION(BlueprintCallable, Category = "Webworks|Cloud") void CloudDelete(const FString& FileName);
	UFUNCTION(BlueprintPure, Category = "Webworks|Cloud") FString GetLastCloudFile() const { return LastCloudFile; }
	UFUNCTION(BlueprintPure, Category = "Webworks|Cloud") FString GetLastCloudData() const { return LastCloudData; }

private:
	void EnsureSession();
	void RefreshProfile();
	void LoadPersistedIdentity();
	void SaveAccountToken();
	void ClearSavedAccountToken();
	FString DeviceKey();

	void HandleJson(const FString& Verb, const FString& Path, const TSharedPtr<FJsonObject>& Body, TFunction<void(bool, const TSharedPtr<FJsonObject>&)> OnDone);
	void ApplyAccount(const TSharedPtr<FJsonObject>& AccountObj);
	FString ApiUrl(const FString& Path) const;

	UPROPERTY() UWebworksSettings* Settings = nullptr;

	FString AccountToken;
	FString SessionToken;
	FString CurrentSessionId;
	FString LastCloudFile;
	FString LastCloudData;
	FWebworksAccount Account;
	TArray<FWebworksFriend> Friends;
	bool bSessionActive = false;
	FTimerHandle PresenceTimer;
};

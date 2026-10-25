#pragma once

#include "CoreMinimal.h"
#include "WebworksTypes.generated.h"

USTRUCT(BlueprintType)
struct FWebworksAccount
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString Username;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString DisplayName;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString SteamId;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") bool bHasAvatar = false;
};

USTRUCT(BlueprintType)
struct FWebworksFriend
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString Username;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString DisplayName;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString SteamId;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") bool bOnline = false;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString AppId;
};

USTRUCT(BlueprintType)
struct FWebworksFriendList
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") TArray<FWebworksFriend> Friends;
};

USTRUCT(BlueprintType)
struct FWebworksSessionInfo
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString LobbyId;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString OwnerId;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") int32 MaxMembers = 0;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") int32 MemberCount = 0;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString Visibility;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") TMap<FString, FString> Data;
};

USTRUCT(BlueprintType)
struct FWebworksSessionList
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") TArray<FWebworksSessionInfo> Sessions;
};

USTRUCT(BlueprintType)
struct FWebworksLeaderboardEntry
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString SteamId;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString Name;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") int32 Rank = 0;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") int32 Score = 0;
};

USTRUCT(BlueprintType)
struct FWebworksLeaderboard
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, Category = "Webworks") FString Name;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") bool bFound = false;
	UPROPERTY(BlueprintReadOnly, Category = "Webworks") TArray<FWebworksLeaderboardEntry> Entries;
};

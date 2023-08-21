using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using System;

using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using GooglePlayGames.BasicApi.Events;

public class GPGSManager
{
    static GPGSManager _instance = new GPGSManager();
    public static GPGSManager Instance
    {
        get => _instance;
    }

    ISavedGameClient SavedGame => PlayGamesPlatform.Instance.SavedGame;
    IEventsClient Events => PlayGamesPlatform.Instance.Events;

    void Initialize()
    {
        PlayGamesClientConfiguration config =  new PlayGamesClientConfiguration.Builder().EnableSavedGames().Build();
        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
    }

    ///<summary> 로그인 </summary>
    public void Login(Action<bool, ILocalUser> onLoginSuccess = null)
    {
        Initialize();
        PlayGamesPlatform.Instance.Authenticate(SignInInteractivity.CanPromptAlways, (success) => 
        {
            //널 검사 연산자
            onLoginSuccess?.Invoke(success == SignInStatus.Success, Social.localUser);
        });
    }
    ///<summary> 구글 로그아웃 </summary>
    public void Logout() => PlayGamesPlatform.Instance.SignOut();

    public void SaveCloud(string fileName, byte[] saveData, Action<bool> onCloudSaved = null)
    {
        SavedGame.OpenWithAutomaticConflictResolution(fileName, DataSource.ReadCacheOrNetwork, 
          ConflictResolutionStrategy.UseLastKnownGood, (status, game) =>
          {
             var update = new SavedGameMetadataUpdate.Builder().Build();
             SavedGame.CommitUpdate(game, update, saveData, (status2, game2) =>
             {
                onCloudSaved?.Invoke(status2 == SavedGameRequestStatus.Success);
             });
          });
    }

    public void LoadCloud(string fileName, Action<bool, byte[]> onCloudLoaded = null)
    {
        SavedGame.OpenWithAutomaticConflictResolution(fileName, DataSource.ReadCacheOrNetwork, 
            ConflictResolutionStrategy.UseLastKnownGood, (cloudLoadStatus, gameData) =>
            {
                if(cloudLoadStatus == SavedGameRequestStatus.Success)
                {
                    SavedGame.ReadBinaryData(gameData, (binaryReadStatus, loadedData) =>
                    {
                        if(binaryReadStatus == SavedGameRequestStatus.Success)
                        {
                            onCloudLoaded?.Invoke(true, loadedData);
                        }
                        else
                            onCloudLoaded?.Invoke(false, null);
                    });
                }
            });
    }

    public void DeleteCloud(string fileName, Action<bool> onCloudDeleted = null)
    {
        SavedGame.OpenWithAutomaticConflictResolution(fileName, 
            DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime, (status, game) =>
            {
                if(status == SavedGameRequestStatus.Success)
                {
                    SavedGame.Delete(game);
                    onCloudDeleted?.Invoke(true);
                }
                else
                    onCloudDeleted?.Invoke(false);
            });
    }
}
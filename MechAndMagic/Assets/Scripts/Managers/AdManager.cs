using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

public class AdManager : MonoBehaviour
{
    static AdManager _instance = null;
    public static AdManager instance
    {
        get
        {
            if(_instance == null)
            {
                GameObject container = new GameObject();
                container.name = "Ad Manager";
                _instance = container.AddComponent<AdManager>();
                container.transform.SetParent(GameManager.instance.transform);
            }

            return _instance;
        }
    }

    ///<summary> 보상형 광고 ID </summary>
    string rewardAdId = "ca-app-pub-3940256099942544/5224354917";
    RewardedAd rewardedAd;
    ///<summary> 전면 광고 ID </summary>
    string interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
    InterstitialAd interstitialAd;

    ///<summary> 광고 정보 불러오기, GameManager instance 생성 시 호출 </summary>
    public void Initialize()
    {
        #if UNITY_ANDROID
        rewardAdId = "ca-app-pub-3940256099942544/5224354917";
        interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
        #endif
        MobileAds.Initialize(initStatus => { });
        LoadRewardAd();
        LoadInterstitialAd();
    }

    ///<summary> 보상형 광고 생성 </summary>
    void LoadRewardAd()
    {
        rewardedAd = new RewardedAd(rewardAdId);
        rewardedAd.OnAdLoaded += HandleRewardedAdLoaded;
        rewardedAd.OnAdFailedToLoad += FailedToLoad;
        rewardedAd.OnAdOpening += AdOpening;
        rewardedAd.OnAdFailedToShow += FailedToShow;
        rewardedAd.OnUserEarnedReward += EarnedReward;
        rewardedAd.OnAdClosed += RewardAdClosed;

        AdRequest request = new AdRequest.Builder().Build();
        rewardedAd.LoadAd(request);
    }
    ///<summary> 전면 광고 생성 </summary>
    void LoadInterstitialAd()
    {
        interstitialAd = new InterstitialAd(interstitialAdId);
        AdRequest request = new AdRequest.Builder().Build();
        interstitialAd.OnAdClosed += InterstitialAdClosed;
        interstitialAd.LoadAd(request);
    }

    ///<summary> 보상형 광고 보여주기 </summary>
    ///<param name="onEarned"> 광고 시청 완료 시 호출할 이벤트 </param>
    public void ShowRewardAd(EventHandler<Reward> onEarned)
    {
        rewardedAd.OnUserEarnedReward += onEarned;
        StartCoroutine(RewardAdCoroutine());
    }
    ///<summary> 전면 광고 보여주기 </summary>
    public void ShowInterstitialAd() => StartCoroutine(InterstitialAdCoroutine());
    
    ///<summary> 보상형 광고 보여주기, 아직 로드 안 된 경우, 대기 </summary>
    IEnumerator RewardAdCoroutine()
    {
        while(!rewardedAd.IsLoaded())
            yield return null;

        rewardedAd.Show();
    }
    ///<summary> 전면 광고 보여주기, 아직 로드 안 된 경우, 대기 </summary>
    IEnumerator InterstitialAdCoroutine()
    {
        while(!interstitialAd.IsLoaded())
            yield return null;

        interstitialAd.Show();
    }

    public void HandleRewardedAdLoaded(object sender, EventArgs args) 
    {
        Debug.Log("ad loaded");
    }
    public void FailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log($"Failed to load");
    }
    public void AdOpening(object sender, EventArgs args)
    {
        Debug.Log("ad open");
    }   
    public void FailedToShow(object sender, AdErrorEventArgs args)
    {
        Debug.Log("failed to show");
    }
    public void EarnedReward(object sender, Reward args)
    {
        Debug.Log("earn reward");
    }
    void RewardAdClosed(object sender, EventArgs args) => LoadRewardAd();
    void InterstitialAdClosed(object sender, EventArgs args) => LoadInterstitialAd();
}

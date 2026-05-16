//#define ENABLE_ADS
namespace Luzart
{
#if ENABLE_ADS
    using BG_Library.NET;
#endif
    using System;
    public class AdsWrapperManager 
    {
        public static bool isShowPopUp = false;
        private static int countInter = 0;
        public static void ShowReward(string where = null, Action onDone = null, Action onFail = null)
        {
#if ENABLE_ADS
            if (!AdsManager.IsRewardedReady())
            {
                onFail?.Invoke();
                return;
            }
            AdsManager.ShowRewardVideo(where, onDone);
#else
            onDone?.Invoke();
#endif
        }
        public static void ShowInter(string where, Action onDone)
        {
            Action onDoneShow = () =>
            {
                GameUtil.StepToStep(new Action<Action>[]
                {
                    ShowUIBundle,
                    OnDoneAction,
                });
            };
#if ENABLE_ADS
            //if (DataWrapperGame.CurrentLevel >= GameCustom.Ins.RemoteConfigCustom.levelShowAdsInter)
            {
                GameUtil.Log(where);
                AdsManager.ShowInterstitial(where, onDoneShow);
            }
            //else
#endif
            {
                OnDoneAction();
            }
            void OnDoneAction(Action onActionDone = null)
            {
                onDone?.Invoke();
            }
        }
        private static void ShowUIBundle(Action onDone)
        {
            onDone?.Invoke();
            if (countInter == 0)
            {
                isShowPopUp = true;
            }
            countInter++;
            if(countInter >= 3)
            {
                countInter = 0;
            }
        }
        public static void PurchaseRemoveAds()
        {
#if ENABLE_ADS
            AdsManager.Ins.PurchaseRemoveAds();
#endif
            // PackManager.Instance?.SaveBuyPack("lorry.color.sort.puzzle.removeads"); // PackManager deleted with ads
        }
    }
    public static class KeyAds
    {
        // Reward
        public const string AdsGetBooster0 = "ads_get_booster_0";
        public const string AdsGetBooster1 = "ads_get_booster_1";
        public const string AdsGetBooster2 = "ads_get_booster_2";
        public const string AdsUIAddCoin = "ads_ui_add_coin";
        public const string AdsUIAddHeart = "ads_ui_add_heart";
        public const string AdsUITimeOut = "ads_ui_time_out";
        public const string AdsClaimStarterPack = "ads_claim_starterpack";
        public const string AdsX2CoinWinGame = "ads_x2_coin_win_game";
        public const string AdsSecondChance = "ads_second_chance";
        public const string ObstacleinGame = "obstacleInGame";
        public const string AdsReceivedDailyReward = "ads_received_daily_reward";
        public const string AdsReceivedDailyRewardMissed = "ads_received_daily_reward_missed";
        public const string AdsReceivedDailyQuest = "ads_received_daily_quest";
        public const string AdsJourneyToSuccess = "ads_journey_to_success";
        public const string AdsSpin = "ads_spin";
        // Inter
        public const string AdsEndLevel = "ads_end_level";
        public const string CompleteStage = "ads_complete_Stage";
        public const string CompleteStage2 = "ads_complete_Stage_2";
    }
    public enum PositionAds
    {
        continue_win_1 = 0,
        continue_win_2 = 1,
        QuitHome = 1,
    }
}

//#define FIREBASE_LOG
//#define FIREBASE_ENABLE
namespace Luzart
{
    public static class EventNameUserProperties
    {
        public const string Connection = "connection";
        public const string TypeUser = "type";
        public const string BestLevel = "best_level";
    }
    public static class KeyFirebase
    {
        public const string LevelStart = "level_start";
        public const string LevelFailed = "level_fail";
        public const string LevelEnd = "level_end";
        public const string LevelResult = "level_result";
        public const string ResEarn = "res_earn";
        public const string ResSpend = "res_spend";
        public const string ButtonClick = "button_click";
        //
        public const string IAP_Show = "iap_Show";
        public const string BattlePassClaimed = "battle_pass_claimed";
        public const string FlightEnduranceStart = "flight_endurance_start";
        public const string FlightEnduranceClaimed = "flight_endurance_claimed";
        public const string BattlePassPremiumClick = "battle_pass_premium_click";
        public const string BattlePassUnlockPremium = "battle_pass_unlock_premium";
        //
        public const string DailyLoginReceived = "event_daily_login_received";
        public const string LuckySpinReceived = "event_lucky_spin_receive";
        public const string TicketTallyClaimed = "event_ticket_tally_claimed";
        public const string JourneyToSuccessClaimed = "event_journey_to_success_claimed";
        //
        public const string Event_Show = "event_show";
        public const string Event_Claimed = "event_claimed";
        public const string Event_Completed = "event_completed";
    }
    public static class TypeFirebase
    {
        public const string LevelID = "level_id";
        public const string Amount = "amount";
        public const string Home = "home";
        public const string Type = "type";
        public const string Location = "location";
        //
        public const string LevelReward = "level_reward";
        public const string LevelDifficulty = "level_difficulty";
        public const string LevelProgress = "level_progress";
        public const string LevelTime = "level_time";
        public const string LevelPlaytime = "level_playtime";
        public const string RemainingSeat = "remaining_seats";
        public const string RemainingPeople = "remaining_people";
        public const string ExtraTimePurchased = "extra_time_purchased";
        public const string TimeBooster = "time_booster";
        public const string JumpBooster = "jump_booster";
        public const string AreaBooster = "area_booster";
        public const string AmountEarn = "amount_earn";
        public const string AmountSpend = "amount_spend";
        public const string EventType = "event_type";
        public const string TypeRes = "type";
        public const string TimeAdd = "time_add";
        public const string IDEvent = "id_event";
        public const string Duration = "duration";
        public const string Players = "players";
        public const string Rewards = "rewards";
        public const string PopUpUnder = "popup_under";
        public const string PopUp = "popup";
        public const string Button = "button_iap";
        //
        public const string EventName = "event_name";
        public const string EventDuration = "event_duration";
    }
    public static class ValueFirebase
    {
        public const string Normal = "normal";
        public const string Hard = "hard";
        public const string UltraHard = "ultra_hard";
        public const string FirstTime = "first_time";
        // Location
        public const string LevelFinish = "level_finish";
        public const string LevelFinishDoubleCoin = "level_doubleCoin";
        public const string BattlePassFree = "batllePass_free";
        public const string BattlePassPay = "batllePass_pay";
        public const string RefillYourGold = "refill_your_gold";
        public const string RewardAds = "reward_ads";
        public const string TicketTally = "ticket_tally";
        public const string JourneyToSuccess = "journey_to_success";
        public const string LimitTimeEvent = "limit_time_event";
        public const string DailyReward = "daily_reward";
        //
        public const string ReFillHeart = "refill_heart";
        public const string TimeOut = "time_out";
        public const string TimeBooster = "time_booster";
        public const string JumpBooster = "jump_booster";
        public const string AreaBooster = "area_booster";
        public const string TimeOutTicket = "time_out_ticket";
        public const string TimeOutAreYouSure = "time_out_are_you_sure";
        //
        public const string Btn_HomeHeart = "heart";
        public const string Btn_HomeCoin = "coin";
        public const string Btn_HomeStarterPack = "starter_pack";
        public const string Btn_HomeVIPPack = "VIP_pack";
        public const string Btn_HomeMiniPack = "mini_pack";
        public const string Btn_HomeLargePack = "large_pack";
        public const string Btn_HomeSuperPack = "super_pack";
        public const string Btn_HomeLifeAndCoinPack = "lifeandcoin_pack";
        public const string Btn_HomeValentinePack = "valentine_pack";
        public const string Btn_HomeNoMoreAdsPack = "no_more_ads_pack";
        public const string Btn_HomeTicketTally = "ticket_tally";
        public const string Btn_HomeBattlePass = "batlle_pass";
        public const string Btn_HomeJourneyToSuccess = "journey_to_success";
        public const string Btn_RiseOfKittens = "rise_of_kittens";
        public const string Btn_Cup = "cup";
        //
        public const string OpenApp = "open_app";
        public const string Home = "home";
        public const string InGame = "ingame";
        public const string Shop = "shop";
        public const string Settings = "settings";
        public const string BattlePassPopUp = "battlePass";
        //
        public const string AutoShow_NoMoreAdsPack = "no_more_ads";
        public const string AutoShow_Heart = "heart";
        public const string AutoShow_Coin = "coin";
        public const string AutoShow_BoosterPack = "booster_pack";
        public const string AutoShow_TicketTally = "ticket_tally";
        public const string AutoShow_Ratting = "ratting";
        public const string AutoShow_LevelFail = "level_fail";
        public const string AutoShow_LimitTimeEvent = "limit_time_event";
        //
        public const string Free = "free";
        public const string Pay = "pay";
        public const string Ads = "ads";
        //
        public const string PackStarter = "pack_starter";
        public const string PackMini = "pack_mini";
        public const string PackLarge = "pack_large";
        public const string PackSuper = "pack_super";
        public const string PackVIP = "pack_vip";
        public const string PackLargeAndCoin = "pack_large_and_coin";
        public const string PackStarterAds = "pack_starter_ads";
        public const string PackNoAds = "pack_no_ads";
        public const string PackBattlePass = "pack_battlePass";
        //
        public const string HeartAutoAdd = "heart_auto_add";
        //
        public const string FlightEnduranceReceive = "flight_endurance_receive";
        public const string RiseOfKittensReceive = "rise_of_kittens_receive";
        public const string CaptainTrophyReceive = "captain_trophy_receive";
        //
        public const string UIAddCoin = "ui_add_coin";
        public const string RewardHeartAds = "reward_heart_ads";
        public const string OnContinueByCoin = "on_continue_by_coin";
        public const string OnDoubleCoinWin = "on_double_coin_win";
        public const string ResDailyReward = "received_daily_reward";
        public const string ResDailyQuest = "received_daily_quest";
        public const string ResDailyQuestProcess = "received_daily_quest_process";
        public const string ResX2CoinWinGame = "x2_coin_win_game";
        public const string Tutorial = "tutorial";
        // 
        public const string LuckySpinReceived = "lucky_spin_received";
        // 
        public const string WinStreakReceived = "win_streak_received";
        //
        public const string RacingReceived = "racing_received";
    }
}

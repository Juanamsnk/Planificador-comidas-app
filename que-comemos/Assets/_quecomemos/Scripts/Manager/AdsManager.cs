using GoogleMobileAds.Api;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    private static AdsManager instance;
    private BannerView bannerView;
    private bool bannerReady = false;

    private string adUnitIdAndroid = "ca-app-pub-6192873534407725/6799197234";

    public static AdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AdsManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("AdsManager");
                    instance = obj.AddComponent<AdsManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeMobileAds();
        LoadBannerAd();
    }

    private void InitializeMobileAds()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("Google Mobile Ads inicializado");
        });
    }

    private void LoadBannerAd()
    {
        var adRequest = new AdRequest();

#if UNITY_ANDROID
        string adUnitId = adUnitIdAndroid;
#elif UNITY_IOS
            string adUnitId = adUnitIdIOS;
#else
            string adUnitId = "unused";
#endif

        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner cargado correctamente!");
            bannerReady = true;
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Error cargando banner: " + error.GetMessage());
        };

        Debug.Log("Cargando banner...");
        bannerView.LoadAd(adRequest);
    }

    public void ShowBanner()
    {
        if (!bannerReady)
        {
            Debug.LogWarning("Banner aún no está listo");
            return;
        }

        if (bannerView != null)
        {
            bannerView.Show();
            Debug.Log("Banner mostrado");
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
            bannerView.Hide();
    }

    private void OnDestroy()
    {
        bannerView?.Destroy();
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileLevelView : MonoBehaviour
{
    [SerializeField] private GeneralParameters _generalParameters;
    [SerializeField] private ProfileProgressConfig _progressConfig;
    [SerializeField] private GearPacksConfig _gearPacksConfig;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _experienceText;
    [SerializeField] private Slider _experienceSlider;
    [SerializeField] private Button _openPopupButton;

    private ProfileLevelPopup _levelUpPopup;

    private void Awake()
    {
        ProfileManager.SetGeneralParameters(_generalParameters);

        if (_openPopupButton != null)
            _openPopupButton.onClick.AddListener(OpenPopup);
    }

    private void Start()
    {
        Debug.Log($"[RewardDebug] ProfileLevelView.Start. ui={Global.UIController != null}, config={_gearPacksConfig != null}, hasPendingLevelUp={ProfileManager.HasPendingLevelUp}, commonPacks={GearPackManager.GetPackCount(GearPackRarity.Common)}, uncommonPacks={GearPackManager.GetPackCount(GearPackRarity.Uncommon)}");

        if (OpenPendingLevelUpPopup() == false)
            OpenEraTransitionOrPacks();
    }

    private void OnEnable()
    {
        ProfileManager.ProfileChanged += UpdateView;
        ProfileManager.LevelUp += OnLevelUp;
        UpdateView();
    }

    private void OnDisable()
    {
        ProfileManager.ProfileChanged -= UpdateView;
        ProfileManager.LevelUp -= OnLevelUp;
    }

    private void OnDestroy()
    {
        if (_openPopupButton != null)
            _openPopupButton.onClick.RemoveListener(OpenPopup);
    }

    private void UpdateView()
    {
        if (_levelText != null)
            _levelText.text = $"LVL {ProfileManager.Level}";

        if (_experienceText != null)
            _experienceText.text = $"{ProfileManager.Experience}/{ProfileManager.RequiredExperience}";

        if (_experienceSlider != null)
        {
            _experienceSlider.minValue = 0f;
            _experienceSlider.maxValue = ProfileManager.RequiredExperience;
            _experienceSlider.value = ProfileManager.Experience;
        }
    }

    private void OpenPopup()
    {
        ProfileLevelPopup popup = Global.UIController != null ? Global.UIController.Show<ProfileLevelPopup>() : null;

        if (popup != null)
            popup.Initialize(_progressConfig, false);
    }

    private void OnLevelUp(int level)
    {
        if (OpenLevelUpPopup(true))
            ProfileManager.TryConsumePendingLevelUp(out _);
    }

    private bool OpenPendingLevelUpPopup()
    {
        if (ProfileManager.HasPendingLevelUp == false)
            return false;

        if (OpenLevelUpPopup(true) == false)
            return false;

        ProfileManager.TryConsumePendingLevelUp(out _);
        return true;
    }

    private bool OpenLevelUpPopup(bool openPacksAfterAnimation)
    {
        ProfileLevelPopup popup = Global.UIController != null ? Global.UIController.Show<ProfileLevelPopup>() : null;

        if (popup == null)
            return false;

        if (openPacksAfterAnimation)
        {
            if (_levelUpPopup != null)
                _levelUpPopup.LevelUpAnimationCompleted -= OnLevelUpAnimationCompleted;

            _levelUpPopup = popup;
            popup.LevelUpAnimationCompleted += OnLevelUpAnimationCompleted;
        }

        popup.Initialize(_progressConfig, true);
        return true;
    }

    private void OnLevelUpAnimationCompleted()
    {
        if (_levelUpPopup != null)
        {
            _levelUpPopup.LevelUpAnimationCompleted -= OnLevelUpAnimationCompleted;
            _levelUpPopup.Hide();
            _levelUpPopup = null;
        }

        StartCoroutine(OpenPacksAfterLevelPopupHide());
    }

    private System.Collections.IEnumerator OpenPacksAfterLevelPopupHide()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        OpenEraTransitionOrPacks();
    }

    private void OpenEraTransitionOrPacks()
    {
        Debug.Log("[RewardDebug] ProfileLevelView.OpenEraTransitionOrPacks.");

        if (EraTransitionManager.TryOpenPendingPopup(OpenPendingPacks))
            return;

        OpenPendingPacks();
    }

    private void OpenPendingPacks()
    {
        Debug.Log($"[RewardDebug] ProfileLevelView.OpenPendingPacks. config={_gearPacksConfig != null}, ui={Global.UIController != null}, commonPacks={GearPackManager.GetPackCount(GearPackRarity.Common)}, uncommonPacks={GearPackManager.GetPackCount(GearPackRarity.Uncommon)}");
        GearPackManager.TryOpenPendingRewardPopups(_gearPacksConfig);
    }
}

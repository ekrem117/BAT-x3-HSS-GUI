namespace BatX3_HSS_GUI.Application.Parameters.UI
{
    public sealed class StaticParameterUiCatalog :
        IParameterUiCatalog
    {
        private static readonly IReadOnlyList<ParameterOption>
            SystemLinkOptions =
            [
                new ParameterOption(
                    0,
                    "Bağlantı Yok"),

                new ParameterOption(
                    1,
                    "Bağlı"),

                new ParameterOption(
                    2,
                    "Zaman Aşımı")
            ];

        private static readonly IReadOnlyList<ParameterOption>
            WeaponArmedOptions =
            [
                new ParameterOption(
                    0,
                    "Kapalı"),

                new ParameterOption(
                    1,
                    "Açık")
            ];

        private static readonly IReadOnlyList<ParameterOption>
           SystemModeOptions =
           [
               new ParameterOption(0, "Idle"),
               new ParameterOption(1, "Manual"),
               new ParameterOption(2, "Auto"),
               new ParameterOption(3, "Loop Demo"),
               new ParameterOption(4, "E Stop")
           ];

        private static readonly IReadOnlyList<ParameterOption>
            FireModeOptions =
            [
                new ParameterOption(
                    0,
                    "Tekli"),

                new ParameterOption(
                    1,
                    "Seri")
            ];

        private static readonly IReadOnlyList<ParameterOption>
            WeaponSelectionOptions =
            [
                new ParameterOption(
                    0,
                    "Sol"),

                new ParameterOption(
                    1,
                    "Sağ"),

                new ParameterOption(
                    2,
                    "Her İkisi")
            ];

        private readonly IReadOnlyDictionary<
            string,
            ParameterUiDefinition> _definitions;

        public StaticParameterUiCatalog()
        {
            ParameterUiDefinition[] definitions =
            [
                // ========================================================
                // OPERATION - SYSTEM STATUS
                // ========================================================

                new()
                {
                    ParameterName =
                        ParameterNames.System.Link,

                    DisplayName =
                        "Kontrolcü Bağlantısı",

                    ControlType =
                        ParameterControlType.ReadOnly,

                    GroupName =
                        ParameterUiSections.SystemStatus,

                    DisplayOrder =
                        10,

                    Options =
                        SystemLinkOptions,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Kontrolcü ile mevcut haberleşme durumunu gösterir.",

                    IsProminent =
                        true
                },

                new()
                {
                    ParameterName =
                        ParameterNames.System.Fps,

                    DisplayName =
                        "Video Kare Hızı",

                    ControlType =
                        ParameterControlType.ReadOnly,

                    GroupName =
                        ParameterUiSections.SystemStatus,

                    DisplayOrder =
                        20,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Canlı video akışının anlık kare hızını gösterir."
                },              

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.DetectionCount,

                    DisplayName =
                        "Tespit Sayısı",

                    ControlType =
                        ParameterControlType.ReadOnly,

                    GroupName =
                        ParameterUiSections.SystemStatus,

                    DisplayOrder =
                        30,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Son işlenen karede bulunan hedef sayısını gösterir."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.InferenceMilliseconds,

                    DisplayName =
                        "Çıkarım Süresi",

                    ControlType =
                        ParameterControlType.ReadOnly,

                    GroupName =
                        ParameterUiSections.SystemStatus,

                    DisplayOrder =
                        40,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Görüntü işleme modelinin son çıkarım süresini milisaniye olarak gösterir."
                },

                // ========================================================
                // OPERATION - DETECTION
                // ========================================================

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.ConfidenceThreshold,

                    DisplayName =
                        "Tespit Güven Eşiği",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.Detection,

                    DisplayOrder =
                        10,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Bir nesne tespitinin kabul edilmesi için gereken minimum güven değerini belirler.",

                    IsProminent =
                        true
                },

                // ========================================================
                // OPERATION - WEAPON
                // ========================================================

                new()
                {
                    ParameterName =
                        ParameterNames.Weapon.Armed,

                    DisplayName =
                        "Silah Yetkisi",

                    ControlType =
                        ParameterControlType.Select,

                    GroupName =
                        ParameterUiSections.Weapon,

                    DisplayOrder =
                        10,

                    Options =
                        WeaponArmedOptions,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Silah sisteminin ateş yetkisini açar veya kapatır.",

                    IsProminent =
                        true
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Weapon.FireMode,

                    DisplayName =
                        "Atış Modu",

                    ControlType =
                        ParameterControlType.Select,

                    GroupName =
                        ParameterUiSections.Weapon,

                    DisplayOrder =
                        20,

                    Options =
                        FireModeOptions,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Tekli veya seri atış modunu seçer.",

                    IsProminent =
                        true
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Weapon.BurstCount,

                    DisplayName =
                        "Seri Atış Adedi",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.Weapon,

                    DisplayOrder =
                        30,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Seri atış modunda gerçekleştirilecek atış sayısını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Weapon.Selected,

                    DisplayName =
                        "Seçili Namlu",

                    ControlType =
                        ParameterControlType.Select,

                    GroupName =
                        ParameterUiSections.Weapon,

                    DisplayOrder =
                        40,

                    Options =
                        WeaponSelectionOptions,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Atışta kullanılacak namlu veya namluları seçer.",

                    IsProminent =
                        true
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Weapon.Ammo,

                    DisplayName =
                        "Kalan Mermi",

                    ControlType =
                        ParameterControlType.ReadOnly,

                    GroupName =
                        ParameterUiSections.Weapon,

                    DisplayOrder =
                        50,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Mevcut kalan mühimmat miktarını gösterir.",

                    IsProminent =
                        true
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Weapon.ShotsFired,

                    DisplayName =
                        "Toplam Atış",

                    ControlType =
                        ParameterControlType.ReadOnly,

                    GroupName =
                        ParameterUiSections.Weapon,

                    DisplayOrder =
                        60,

                    Page =
                        ParameterUiPage.Operation,

                    Description =
                        "Sistem tarafından gerçekleştirilen toplam atış sayısını gösterir."
                },

                // ========================================================
                // CONFIGURATION - ENEMY COLOR CALIBRATION
                // ========================================================

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.EnemyHMin,

                    DisplayName =
                        "Ton Alt Eşiği 1",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.EnemyColorCalibration,

                    DisplayOrder =
                        10,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Düşman renk analizinin birinci ton aralığı alt sınırını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.EnemyHMax,

                    DisplayName =
                        "Ton Üst Eşiği 1",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.EnemyColorCalibration,

                    DisplayOrder =
                        20,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Düşman renk analizinin birinci ton aralığı üst sınırını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.EnemyHMin2,

                    DisplayName =
                        "Ton Alt Eşiği 2",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.EnemyColorCalibration,

                    DisplayOrder =
                        30,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Düşman renk analizinin ikinci ton aralığı alt sınırını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.EnemyHMax2,

                    DisplayName =
                        "Ton Üst Eşiği 2",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.EnemyColorCalibration,

                    DisplayOrder =
                        40,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Düşman renk analizinin ikinci ton aralığı üst sınırını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.EnemySMin,

                    DisplayName =
                        "Minimum Doygunluk",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.EnemyColorCalibration,

                    DisplayOrder =
                        50,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Düşman renk analizinde kullanılacak minimum doygunluk değerini belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.EnemyVMin,

                    DisplayName =
                        "Minimum Parlaklık",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.EnemyColorCalibration,

                    DisplayOrder =
                        60,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Düşman renk analizinde kullanılacak minimum parlaklık değerini belirler."
                },

                // ========================================================
                // CONFIGURATION - FRIEND COLOR CALIBRATION
                // ========================================================

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.FriendHMin,

                    DisplayName =
                        "Ton Alt Eşiği",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.FriendColorCalibration,

                    DisplayOrder =
                        10,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Dost renk analizinin ton aralığı alt sınırını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.FriendHMax,

                    DisplayName =
                        "Ton Üst Eşiği",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.FriendColorCalibration,

                    DisplayOrder =
                        20,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Dost renk analizinin ton aralığı üst sınırını belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.FriendSMin,

                    DisplayName =
                        "Minimum Doygunluk",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.FriendColorCalibration,

                    DisplayOrder =
                        30,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Dost renk analizinde kullanılacak minimum doygunluk değerini belirler."
                },

                new()
                {
                    ParameterName =
                        ParameterNames.Vision.FriendVMin,

                    DisplayName =
                        "Minimum Parlaklık",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.FriendColorCalibration,

                    DisplayOrder =
                        40,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Dost renk analizinde kullanılacak minimum parlaklık değerini belirler."
                },

                // ========================================================
                // CONFIGURATION - CONTROL
                // ========================================================

                new()
                {
                    ParameterName =
                        ParameterNames.Control.PidKp,

                    DisplayName =
                        "Oransal Kazanç (Kp)",

                    ControlType =
                        ParameterControlType.Numeric,

                    GroupName =
                        ParameterUiSections.Control,

                    DisplayOrder =
                        10,

                    Page =
                        ParameterUiPage.Configuration,

                    Description =
                        "Takip kontrolcüsünün oransal PID kazancını belirler."
                }
            ];

            _definitions =
                definitions.ToDictionary(
                    definition =>
                        definition.ParameterName,
                    StringComparer.Ordinal);
        }

        public IReadOnlyCollection<ParameterUiDefinition> GetAll()
        {
            return _definitions
                .Values
                .OrderBy(
                    definition =>
                        definition.Page)
                .ThenBy(
                    definition =>
                        definition.GroupName,
                    StringComparer.CurrentCulture)
                .ThenBy(
                    definition =>
                        definition.DisplayOrder)
                .ToArray();
        }

        public IReadOnlyCollection<ParameterUiDefinition> GetByPage(
            ParameterUiPage page)
        {
            return _definitions
                .Values
                .Where(
                    definition =>
                        definition.Page == page)
                .OrderBy(
                    definition =>
                        definition.GroupName,
                    StringComparer.CurrentCulture)
                .ThenBy(
                    definition =>
                        definition.DisplayOrder)
                .ToArray();
        }

        public bool TryGet(
            string parameterName,
            out ParameterUiDefinition? definition)
        {
            return _definitions.TryGetValue(
                parameterName,
                out definition);
        }

        public ParameterUiDefinition GetRequired(
            string parameterName)
        {
            if (!_definitions.TryGetValue(
                    parameterName,
                    out ParameterUiDefinition? definition))
            {
                throw new KeyNotFoundException(
                    $"Parametre UI kataloğunda bulunamadı: {parameterName}");
            }

            return definition;
        }
    }
}
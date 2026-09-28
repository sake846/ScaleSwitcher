using System;
using System.Globalization;
using ScaleSwitcher.Models;

namespace ScaleSwitcher.Services
{
    public enum UiLanguage
    {
        Japanese,
        English
    }

    public sealed class AppLocalization
    {
        private readonly UiLanguage _language;

        public AppLocalization(AppSettings settings)
        {
            _language = ResolveLanguage(settings.UiLanguage);
        }

        public static UiLanguage ResolveLanguage(string? configuredLanguage)
        {
            if (TryParseConfiguredLanguage(configuredLanguage, out var configured))
            {
                return configured;
            }

            return ResolveFromSystemCulture(CultureInfo.CurrentUICulture);
        }

        private static bool TryParseConfiguredLanguage(string? configuredLanguage, out UiLanguage language)
        {
            language = UiLanguage.English;
            if (string.IsNullOrWhiteSpace(configuredLanguage))
            {
                return false;
            }

            var normalized = configuredLanguage.Trim();
            if (string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(normalized, "ja", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "ja-JP", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "japanese", StringComparison.OrdinalIgnoreCase))
            {
                language = UiLanguage.Japanese;
                return true;
            }

            if (string.Equals(normalized, "en", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "en-US", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "en-GB", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "english", StringComparison.OrdinalIgnoreCase))
            {
                language = UiLanguage.English;
                return true;
            }

            return false;
        }

        private static UiLanguage ResolveFromSystemCulture(CultureInfo culture)
        {
            for (var current = culture; current != CultureInfo.InvariantCulture; current = current.Parent)
            {
                if (string.Equals(current.TwoLetterISOLanguageName, "ja", StringComparison.OrdinalIgnoreCase))
                {
                    return UiLanguage.Japanese;
                }
                if (string.Equals(current.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase))
                {
                    return UiLanguage.English;
                }
            }
            return UiLanguage.English;
        }

        public string DisplayPrefix => _language switch
        {
            UiLanguage.English => "Display",
            _ => "ディスプレイ"
        };

        public string PrimaryLabel => _language switch
        {
            UiLanguage.English => "Primary",
            _ => "プライマリ"
        };

        public string Menu_Scale => _language switch
        {
            UiLanguage.English => "Scale",
            _ => "スケーリング"
        };

        public string Menu_Resolution => _language switch
        {
            UiLanguage.English => "Resolution",
            _ => "解像度"
        };

        public string Menu_RunAtStartup => _language switch
        {
            UiLanguage.English => "Run at Windows startup",
            _ => "Windows起動時に実行"
        };

        public string Menu_Settings => _language switch
        {
            UiLanguage.English => "Settings...",
            _ => "設定..."
        };

        public string Menu_ShowDisplayInfo => _language switch
        {
            UiLanguage.English => "Show display info",
            _ => "ディスプレイ情報を表示"
        };

        public string Menu_Exit => _language switch
        {
            UiLanguage.English => "Exit",
            _ => "終了"
        };

        public string Settings_Title => _language switch
        {
            UiLanguage.English => "ScaleSwitcher — Settings",
            _ => "ScaleSwitcher — 設定"
        };

        public string Settings_HeaderDescription => _language switch
        {
            UiLanguage.English => "Configure what to switch when you left-click or use the keyboard shortcut.",
            _ => "左クリックまたはショートカットで切り替える内容を設定します。"
        };

        public string Settings_TargetDisplay => _language switch
        {
            UiLanguage.English => "Target Display (Left Click):",
            _ => "対象のディスプレイ (左クリック時):"
        };

        public string Settings_Scales => _language switch
        {
            UiLanguage.English => "Scales to Cycle",
            _ => "切り替える倍率"
        };

        public string Settings_ScalesDescription => _language switch
        {
            UiLanguage.English => "Select at least one scale to cycle through when switching.",
            _ => "切り替え時にローテーションする倍率を 1 つ以上選んでください。"
        };

        public string Menu_KeyboardSwitch => _language switch
        {
            UiLanguage.English => "Switch using keyboard",
            _ => "キーボードで切り替え"
        };

        public string Menu_KeyboardOff => _language switch
        {
            UiLanguage.English => "Off",
            _ => "オフ"
        };

        public string Menu_KeyboardShift => _language switch
        {
            UiLanguage.English => "Left + Right Shift",
            _ => "左右Shift"
        };

        public string Menu_KeyboardControl => _language switch
        {
            UiLanguage.English => "Left + Right Ctrl",
            _ => "左右Ctrl"
        };

        public string Menu_KeyboardAlt => _language switch
        {
            UiLanguage.English => "Left + Right Alt",
            _ => "左右Alt"
        };

        public string Settings_UseCustomDisplayName => _language switch
        {
            UiLanguage.English => "Rename display",
            _ => "表示名を変更"
        };

        public string Settings_CustomDisplayName => _language switch
        {
            UiLanguage.English => "Display name:",
            _ => "表示する表示名:"
        };

        public string Settings_Save => _language switch
        {
            UiLanguage.English => "Save",
            _ => "保存"
        };

        public string Settings_Cancel => _language switch
        {
            UiLanguage.English => "Cancel",
            _ => "キャンセル"
        };

        public string Settings_About => _language switch
        {
            UiLanguage.English => "About",
            _ => "このアプリについて"
        };

        public string Settings_NoScaleError => _language switch
        {
            UiLanguage.English => "Select at least one scale before saving.",
            _ => "倍率を 1 件以上選んでから保存してください。"
        };

        public string ExitConfirmMessage => _language switch
        {
            UiLanguage.English => "Are you sure you want to exit ScaleSwitcher?",
            _ => "ScaleSwitcherを終了しますか？"
        };
    }
}

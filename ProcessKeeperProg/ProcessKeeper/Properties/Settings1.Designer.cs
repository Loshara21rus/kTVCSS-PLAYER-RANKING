//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Diagnostics;

namespace ProcessKeeper.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings1 : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings1 defaultInstance = ((Settings1)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings1())));
        internal ProcessStartInfo CCW;

        public static Settings1 Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\kTVCSS\\_process\\WorkNode\\WorkNode.exe")]
        public string WorkNode {
            get {
                return ((string)(this["WorkNode"]));
            }
            set {
                this["WorkNode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\kTVCSS\\_process\\kBot\\kBot.exe")]
        public string kurwanatorVkBot {
            get {
                return ((string)(this["kurwanatorVkBot"]));
            }
            set {
                this["kurwanatorVkBot"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\kTVCSS\\_process\\Custom Cup Worker\\CustomCupWorker.exe")]
        public string tsAdminBot {
            get {
                return ((string)(this["CCW"]));
            }
            set {
                this["CCW"] = value;
            }
        }
    }
}

﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Xbim.COBieLite.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("qto_SpaceBaseQuantities.GrossFloorArea;Pset_BaseQuantities.GrossFloorArea;BaseQua" +
            "ntities.GrossFloorArea;Pset_SpaceCommon.GrossPlannedArea")]
        public string SpaceGrossFloorArea {
            get {
                return ((string)(this["SpaceGrossFloorArea"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("qto_SpaceBaseQuantities.NetFloorArea;Pset_BaseQuantities.GrossFloorArea;BaseQuant" +
            "ities.NetFloorArea;Pset_SpaceCommon.NetPlannedArea")]
        public string SpaceNetFloorArea {
            get {
                return ((string)(this["SpaceNetFloorArea"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("qto_SpaceBaseQuantities.FinishCeilingHeight;")]
        public string SpaceFinishedCeilingHeight {
            get {
                return ((string)(this["SpaceFinishedCeilingHeight"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("COBie_Space.RoomTag;this.Name;Pset_SpaceCommon.Reference;Pset_SpaceCommon.RoomTag" +
            "")]
        public string SpaceRoomTag {
            get {
                return ((string)(this["SpaceRoomTag"]));
            }
        }
    }
}

﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.6.81.0.
// 
namespace SqlPad.Oracle {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://husqvik.com/SqlPad/2014/08/Oracle", IsNullable=false)]
    public partial class OracleConfiguration {
        
        private OracleConfigurationExecutionPlan executionPlanField;
        
        private string startupScriptField;
        
        private string tKProfPathField;
        
        private OracleConfigurationConnection[] connectionsField;
        
        /// <remarks/>
        public OracleConfigurationExecutionPlan ExecutionPlan {
            get {
                return this.executionPlanField;
            }
            set {
                this.executionPlanField = value;
            }
        }
        
        /// <remarks/>
        public string StartupScript {
            get {
                return this.startupScriptField;
            }
            set {
                this.startupScriptField = value;
            }
        }
        
        /// <remarks/>
        public string TKProfPath {
            get {
                return this.tKProfPathField;
            }
            set {
                this.tKProfPathField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Connection", IsNullable=false)]
        public OracleConfigurationConnection[] Connections {
            get {
                return this.connectionsField;
            }
            set {
                this.connectionsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationExecutionPlan {
        
        private OracleConfigurationExecutionPlanTargetTable targetTableField;
        
        /// <remarks/>
        public OracleConfigurationExecutionPlanTargetTable TargetTable {
            get {
                return this.targetTableField;
            }
            set {
                this.targetTableField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationExecutionPlanTargetTable {
        
        private string schemaField;
        
        private string nameField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Schema {
            get {
                return this.schemaField;
            }
            set {
                this.schemaField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.81.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationConnection {
        
        private string startupScriptField;
        
        private string connectionNameField;
        
        private string remoteTraceDirectoryField;
        
        /// <remarks/>
        public string StartupScript {
            get {
                return this.startupScriptField;
            }
            set {
                this.startupScriptField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ConnectionName {
            get {
                return this.connectionNameField;
            }
            set {
                this.connectionNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string RemoteTraceDirectory {
            get {
                return this.remoteTraceDirectoryField;
            }
            set {
                this.remoteTraceDirectoryField = value;
            }
        }
    }
}

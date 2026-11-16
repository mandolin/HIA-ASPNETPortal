using System;
using System.Collections.Generic;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>宿主框架封闭词表。当前仅登记项目实际实现的 ASP.NET Web Forms（ASCX）宿主；
    ///   MVC / CMS 等形态尚未落地，不作为可声明值。</zh-CN>
    ///   <en>Closed vocabulary of host frameworks. Only the actually implemented ASP.NET Web Forms (ASCX) host is registered;
    ///   MVC/CMS and other forms are not yet implemented and are therefore not declarable.</en>
    /// </lang>
    /// </summary>
    public static class PortalModuleHostFrameworks
    {
        /// <summary>ASP.NET Web Forms 宿主（当前唯一已实现形态）。</summary>
        public const string AspNetWebForms = "AspNetWebForms";

        /// <summary><lang><zh-CN>精确判定值是否在封闭词表内；大小写与空白变体一律拒绝。</zh-CN><en>Exact membership test; case and whitespace variants are rejected.</en></lang></summary>
        public static bool IsRegistered(string value)
        {
            return string.Equals(value, AspNetWebForms, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>入口形态封闭词表。当前仅登记受控 ASCX 用户控件入口；
    ///   其它入口形态未实现，不作为可声明值。</zh-CN>
    ///   <en>Closed vocabulary of entry forms. Only the controlled ASCX user-control entry is registered;
    ///   other entry forms are not implemented and are therefore not declarable.</en>
    /// </lang>
    /// </summary>
    public static class PortalModuleEntryKinds
    {
        /// <summary>受控 ASCX 用户控件入口（当前唯一已实现形态）。</summary>
        public const string Ascx = "Ascx";

        /// <summary><lang><zh-CN>精确判定值是否在封闭词表内。</zh-CN><en>Exact membership test.</en></lang></summary>
        public static bool IsRegistered(string value)
        {
            return string.Equals(value, Ascx, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <lang>
    ///   <zh-CN>模块形态描述字段的**只读契约校验器**。按 `module.json` 的四类形态维度
    ///   （模块种类 / 宿主框架 / 入口形态 / 资源与依赖声明）做封闭词表校验；
    ///   **不接入装载链、不改变既有包解析结果、不读文件/配置、不写任何内容**。
    ///   字段缺失＝未声明（兼容既有包）；字段出现但值非法＝fail-closed。
    ///   本类零外部依赖：只接收调用方已从 manifest 提取的字符串与字符串集合，
    ///   由调用方负责把 JSON 类型错误映射为 "isString: false"。</zh-CN>
    ///   <en>Read-only contract validator for module form-description fields across the four dimensions
    ///   (module kind / host framework / entry form / resources and dependency declarations). It performs closed-vocabulary
    ///   validation only: it is NOT wired into the loading chain, does NOT change existing package parsing, reads no file/config,
    ///   and writes nothing. A missing field means "undeclared" (backward compatible); a present but invalid value fails closed.
    ///   This class has zero external dependencies: callers pass already-extracted strings and string collections; the caller maps
    ///   JSON type errors to "isString: false".</en>
    /// </lang>
    /// </summary>
    public static class PortalModuleFormContract
    {
        /// <summary><lang><zh-CN>依赖声明数组的最大项数（防止无界声明）。</zh-CN><en>Maximum number of dependency declarations (bounds the array).</en></lang></summary>
        public const int MaxDependencyCount = 64;

        /// <summary><lang><zh-CN>单个依赖声明的最大长度。</zh-CN><en>Maximum length of a single dependency declaration.</en></lang></summary>
        public const int MaxDependencyLength = 100;

        /// <summary><lang><zh-CN>宿主框架字段名。</zh-CN><en>Host framework field name.</en></lang></summary>
        public const string HostFrameworkField = "hostFramework";

        /// <summary><lang><zh-CN>入口形态字段名。</zh-CN><en>Entry form field name.</en></lang></summary>
        public const string EntryKindField = "entryKind";

        /// <summary><lang><zh-CN>依赖声明字段名。</zh-CN><en>Dependency declaration field name.</en></lang></summary>
        public const string DependenciesField = "dependencies";

        /// <summary><lang><zh-CN>模块种类字段名（既有字段，复用不新增）。</zh-CN><en>Module kind field name (existing field, reused).</en></lang></summary>
        public const string ModuleKindField = "moduleKind";

        /// <summary>
        /// <lang>
        ///   <zh-CN>形态字段校验输入；`declared` 表示字段是否在 manifest 出现，`isString` 表示值是否为 JSON 字符串。
        ///   不声明字段（declared=false）时不校验其值，保证既有包兼容。</zh-CN>
        ///   <en>Input for a form-field check: `declared` says whether the field is present in the manifest,
        ///   `isString` says whether the value is a JSON string. Undeclared fields (declared=false) skip value checks for backward compatibility.</en>
        /// </lang>
        /// </summary>
        public struct FieldInput
        {
            /// <summary>字段是否在 manifest 出现。</summary>
            public bool Declared;

            /// <summary>值是否为 JSON 字符串。</summary>
            public bool IsString;

            /// <summary>字段字符串值（仅 IsString=true 时有意义）。</summary>
            public string Value;

            /// <summary><zh-CN>构造已声明的字符串字段。</zh-CN><en>Builds a declared string field.</en></summary>
            public static FieldInput FromString(string value)
            {
                return new FieldInput { Declared = true, IsString = true, Value = value };
            }

            /// <summary><zh-CN>构造已声明的非字符串字段（类型错误）。</zh-CN><en>Builds a declared non-string field (type error).</en></summary>
            public static FieldInput NonString()
            {
                return new FieldInput { Declared = true, IsString = false, Value = null };
            }

            /// <summary><zh-CN>构造未声明字段（兼容既有包）。</zh-CN><en>Builds an undeclared field (legacy compatible).</en></summary>
            public static FieldInput Undeclared()
            {
                return new FieldInput { Declared = false, IsString = false, Value = null };
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>只读校验形态字段。返回 false 时 reasons 至少含一条受控原因；返回 true 时 reasons 为空。
        ///   不修改任何输入、不读文件/配置。调用方须先把 JSON 字段映射为 FieldInput / 字符串集合。</zh-CN>
        ///   <en>Read-only validation of form fields. On false, reasons contains at least one controlled reason; on true it is empty.
        ///   No input is mutated and no file/config is read. Callers must map JSON fields to FieldInput / string collections first.</en>
        /// </lang>
        /// </summary>
        /// <param name="hostFramework">宿主框架字段输入。</param>
        /// <param name="entryKind">入口形态字段输入。</param>
        /// <param name="dependencies">调用方已提取的依赖声明数组；null 表示字段未声明。</param>
        /// <param name="reasons">受控失败原因集合；成功时为空。</param>
        /// <returns>形态契约校验通过返回 true，否则 false。</returns>
        public static bool TryValidate(FieldInput hostFramework, FieldInput entryKind, IList<string> dependencies, out IList<string> reasons)
        {
            var found = new List<string>();

            ValidateEnumField(HostFrameworkField, hostFramework, PortalModuleHostFrameworks.IsRegistered, found);
            ValidateEnumField(EntryKindField, entryKind, PortalModuleEntryKinds.IsRegistered, found);

            if (dependencies != null)
            {
                ValidateDependencies(dependencies, found);
            }

            reasons = found;
            return found.Count == 0;
        }

        private static void ValidateEnumField(string fieldName, FieldInput input, Func<string, bool> isRegistered, IList<string> found)
        {
            if (!input.Declared)
            {
                return;
            }

            if (!input.IsString)
            {
                found.Add(fieldName + " must be a string.");
                return;
            }

            if (!isRegistered(input.Value))
            {
                found.Add(fieldName + " is not a registered value.");
            }
        }

        private static void ValidateDependencies(IList<string> dependencies, IList<string> found)
        {
            if (dependencies.Count > MaxDependencyCount)
            {
                found.Add(DependenciesField + " has too many entries.");
                return;
            }

            for (int i = 0; i < dependencies.Count; i++)
            {
                string value = dependencies[i];
                if (string.IsNullOrWhiteSpace(value) || value.Length > MaxDependencyLength)
                {
                    found.Add(DependenciesField + " item is blank or too long.");
                }
            }
        }
    }
}

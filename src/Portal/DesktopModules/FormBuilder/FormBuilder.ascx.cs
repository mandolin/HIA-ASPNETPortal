using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Unity;

namespace ASPNET.StarterKit.Portal
{
    /// <summary>
    /// <lang>
    ///   <zh-CN>表单构建器模块（独立模块 MVP）：依据可信本地 form.json 渲染表单、采集提交并落库。能力由本体之外以独立模块形式提供，经容纳机制装载运行，不退化成内核模块。</zh-CN>
    ///   <en>Form Builder module (independent-module MVP): renders a form from the trusted local form.json, collects submissions and persists them. The capability is provided externally as an independent module and loaded by the hosting mechanism, without becoming a kernel module.</en>
    /// </lang>
    /// </summary>
    public partial class FormBuilder : PortalModuleControl<FormBuilder>
    {
        /// <summary>表单定义相对路径；随模块包部署于 DesktopModules/FormBuilder/form.json。</summary>
        private const string FormDefinitionPath = "~/DesktopModules/FormBuilder/form.json";

        /// <summary><lang><zh-CN>表单提交数据访问。</zh-CN><en>Form submission data access.</en></lang></summary>
        [Dependency]
        public IFormBuilderDb FormBuilderDb { private get; set; }

        /// <summary><lang><zh-CN>用户数据访问，用于解析当前提交者标识。</zh-CN><en>User data access, used to resolve the current submitter identity.</en></lang></summary>
        [Dependency]
        public IUsersDb UsersDb { private get; set; }

        /// <summary>
        /// <lang>
        ///   <zh-CN>首次加载时渲染表单（标题、提交按钮文案、字段）。</zh-CN>
        ///   <en>On first load, renders the form (title, submit-button text and fields).</en>
        /// </lang>
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            FormDefinition definition = LoadDefinition();
            if (definition == null)
            {
                ShowMessage("Form definition is unavailable.");
                SubmitButton.Visible = false;
                return;
            }

            string locale = GetTwoLetterLocale();
            if (!IsPostBack)
            {
                TitleLiteral.Text = Encode(definition.GetTitle(locale));
                SubmitButton.Text = definition.GetSubmitText(locale);
                FormLiteral.Text = RenderForm(definition, locale, null);
            }
        }

        /// <summary>
        /// <lang>
        ///   <zh-CN>提交处理：采集并校验字段、确认已认证身份、落库并审计；失败时回填已输入值并提示。</zh-CN>
        ///   <en>Submission handler: collects and validates fields, ensures an authenticated identity, persists and audits; on failure re-renders entered values with a message.</en>
        /// </lang>
        /// </summary>
        protected void SubmitButton_Click(object sender, EventArgs e)
        {
            FormDefinition definition = LoadDefinition();
            if (definition == null)
            {
                ShowMessage("Form definition is unavailable.");
                return;
            }

            string locale = GetTwoLetterLocale();
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            foreach (FieldDefinition field in definition.Fields)
            {
                string raw = Context.Request.Form[field.Name] ?? string.Empty;
                string trimmed = raw.Trim();
                if (field.Required && string.IsNullOrEmpty(trimmed))
                {
                    errors.Add(definition.GetLabel(field, locale) + (locale == "zh-CN" ? " 为必填项。" : " is required."));
                    values[field.Name] = raw;
                    continue;
                }
                if (field.MaxLength > 0 && trimmed.Length > field.MaxLength)
                {
                    trimmed = trimmed.Substring(0, field.MaxLength);
                }
                values[field.Name] = trimmed;
            }

            if (errors.Count > 0)
            {
                FormLiteral.Text = RenderForm(definition, locale, values);
                ShowMessage(string.Join(" ", errors));
                return;
            }

            if (!IsAuthenticated())
            {
                FormLiteral.Text = RenderForm(definition, locale, values);
                ShowMessage(locale == "zh-CN" ? "请先登录后再提交。" : "Please sign in before submitting.");
                return;
            }

            int userId = GetCurrentUserId();
            string userName = GetCurrentUserName();
            string dataJson = Serialize(values);

            try
            {
                long id = FormBuilderDb.Submit(new FormBuilderSubmission
                {
                    ModuleId = ModuleId,
                    FormId = definition.FormId,
                    SubmittedBy = userName,
                    SubmittedByUserId = userId,
                    Data = dataJson
                });

                PortalOperationAudit.Record(
                    PortalOperationAuditEvents.BusinessModuleCategory,
                    "FormBuilderSubmitted",
                    "FormBuilder",
                    id.ToString(CultureInfo.InvariantCulture),
                    "Form submitted. FormId=" + definition.FormId + " ModuleId=" + ModuleId.ToString(CultureInfo.InvariantCulture),
                    Context);

                FormLiteral.Text = string.Empty;
                SubmitButton.Visible = false;
                ShowMessage(definition.GetSuccessText(locale));
            }
            catch (Exception)
            {
                FormLiteral.Text = RenderForm(definition, locale, values);
                ShowMessage(locale == "zh-CN" ? "提交失败，请稍后重试。" : "Submission failed. Please try again later.");
            }
        }

        // <lang>
        //   <zh-CN>渲染表单 HTML。定义来自可信本地文件，标签安全；用户输入回显一律 HTML 编码，杜绝 XSS。</zh-CN>
        //   <en>Renders the form HTML. The definition comes from a trusted local file so its labels are safe; any echoed user input is HTML-encoded to prevent XSS.</en>
        // </lang>
        private static string RenderForm(FormDefinition definition, string locale, IDictionary<string, string> values)
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"form-builder-fields\">");
            foreach (FieldDefinition field in definition.Fields)
            {
                string current = values != null && values.ContainsKey(field.Name) ? values[field.Name] : string.Empty;
                string label = Encode(definition.GetLabel(field, locale));
                sb.Append("<div class=\"form-builder-field form-builder-field-").Append(field.Type).Append("\">");
                sb.Append("<label class=\"form-builder-label\" for=\"").Append(field.Name).Append("\">").Append(label);
                if (field.Required)
                {
                    sb.Append(" <span class=\"form-builder-required\" aria-hidden=\"true\">*</span>");
                }
                sb.Append("</label>");

                if (field.Type == "textarea")
                {
                    sb.Append("<textarea class=\"form-builder-input\" id=\"").Append(field.Name)
                      .Append("\" name=\"").Append(field.Name).Append("\" rows=\"5\"></textarea>");
                }
                else if (field.Type == "select")
                {
                    sb.Append("<select class=\"form-builder-input\" id=\"").Append(field.Name)
                      .Append("\" name=\"").Append(field.Name).Append("\">");
                    sb.Append("<option value=\"\"></option>");
                    foreach (OptionDefinition opt in field.Options)
                    {
                        sb.Append("<option value=\"").Append(Encode(opt.Value)).Append("\">").Append(Encode(opt.GetLabel(locale))).Append("</option>");
                    }
                    sb.Append("</select>");
                }
                else if (field.Type == "checkbox")
                {
                    sb.Append("<input class=\"form-builder-input\" type=\"checkbox\" id=\"").Append(field.Name)
                      .Append("\" name=\"").Append(field.Name).Append("\" value=\"true\" />");
                }
                else
                {
                    sb.Append("<input class=\"form-builder-input\" type=\"text\" id=\"").Append(field.Name)
                      .Append("\" name=\"").Append(field.Name).Append("\"");
                    if (field.MaxLength > 0)
                    {
                        sb.Append(" maxlength=\"").Append(field.MaxLength.ToString(CultureInfo.InvariantCulture)).Append("\"");
                    }
                    sb.Append(" />");
                }
                sb.Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private FormDefinition LoadDefinition()
        {
            try
            {
                string path = Server.MapPath(FormDefinitionPath);
                string json = System.IO.File.ReadAllText(path);
                return FormDefinition.Parse(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Serialize(IDictionary<string, string> values)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var pair in values)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append("\"").Append(EscapeJson(pair.Key)).Append("\":\"")
                  .Append(EscapeJson(pair.Value)).Append("\"");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        private string GetTwoLetterLocale()
        {
            string name = System.Threading.Thread.CurrentThread.CurrentUICulture.Name.ToLowerInvariant();
            return name.StartsWith("zh") ? "zh-CN" : "en";
        }

        private bool IsAuthenticated()
        {
            return Context != null && Context.User != null && Context.User.Identity != null && Context.User.Identity.IsAuthenticated;
        }

        private int GetCurrentUserId()
        {
            string userName = GetCurrentUserName();
            if (string.IsNullOrWhiteSpace(userName) || UsersDb == null)
            {
                return 0;
            }
            IUserItem user = UsersDb.GetSingleUser(userName);
            return user == null ? 0 : user.UserId;
        }

        private string GetCurrentUserName()
        {
            return IsAuthenticated() ? Context.User.Identity.Name : string.Empty;
        }

        private void ShowMessage(string message)
        {
            MessageLabel.Text = Server.HtmlEncode(message ?? string.Empty);
        }

        private static string Encode(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
        }
    }

    // <lang>
    //   <zh-CN>表单定义模型（来自可信本地 form.json）：表单、字段、选项、双语文本。</zh-CN>
    //   <en>Form definition model (from the trusted local form.json): form, field, option and bilingual-text types.</en>
    // </lang>
    internal sealed class FormDefinition
    {
        public string FormId { get; set; }
        public LocalizedText Title { get; set; }
        public LocalizedText SubmitText { get; set; }
        public LocalizedText SuccessText { get; set; }
        public List<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();

        public string GetTitle(string locale) => Title?.Get(locale) ?? "Form";
        public string GetSubmitText(string locale) => SubmitText?.Get(locale) ?? "Submit";
        public string GetSuccessText(string locale) => SuccessText?.Get(locale) ?? "Submitted.";
        public string GetLabel(FieldDefinition field, string locale) => field?.Label?.Get(locale) ?? field?.Name ?? string.Empty;

        public static FormDefinition Parse(string json)
        {
            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
            var def = new FormDefinition
            {
                FormId = (string)obj["formId"] ?? "form",
                Title = LocalizedText.Parse(obj["title"]),
                SubmitText = LocalizedText.Parse(obj["submitText"]),
                SuccessText = LocalizedText.Parse(obj["successText"])
            };
            var fields = obj["fields"] as Newtonsoft.Json.Linq.JArray;
            if (fields != null)
            {
                foreach (var f in fields)
                {
                    var field = new FieldDefinition
                    {
                        Name = (string)f["name"],
                        Type = (string)f["type"] ?? "text",
                        Label = LocalizedText.Parse(f["label"]),
                        Required = f["required"] != null && (bool)f["required"],
                        MaxLength = f["maxLength"] != null ? (int)f["maxLength"] : 0
                    };
                    var opts = f["options"] as Newtonsoft.Json.Linq.JArray;
                    if (opts != null)
                    {
                        foreach (var o in opts)
                        {
                            field.Options.Add(new OptionDefinition { Value = (string)o["value"], Label = LocalizedText.Parse(o["label"]) });
                        }
                    }
                    def.Fields.Add(field);
                }
            }
            return def;
        }
    }

    internal sealed class FieldDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public LocalizedText Label { get; set; }
        public bool Required { get; set; }
        public int MaxLength { get; set; }
        public List<OptionDefinition> Options { get; set; } = new List<OptionDefinition>();
    }

    internal sealed class OptionDefinition
    {
        public string Value { get; set; }
        public LocalizedText Label { get; set; }
        public string GetLabel(string locale) => Label?.Get(locale) ?? Value ?? string.Empty;
    }

    internal sealed class LocalizedText
    {
        public string ZhCn { get; set; }
        public string En { get; set; }
        public string Get(string locale) => locale == "zh-CN" ? (ZhCn ?? En) : (En ?? ZhCn);
        public static LocalizedText Parse(Newtonsoft.Json.Linq.JToken token)
        {
            if (token == null)
            {
                return null;
            }
            if (token.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                return new LocalizedText { ZhCn = (string)token, En = (string)token };
            }
            return new LocalizedText { ZhCn = (string)token["zh-CN"], En = (string)token["en"] };
        }
    }
}

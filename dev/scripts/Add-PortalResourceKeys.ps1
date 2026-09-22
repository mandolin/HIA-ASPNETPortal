<#
.SYNOPSIS
<lang>
  <zh-CN>从键文件批量向 App_GlobalResources 写入本地化资源键。</zh-CN>
  <en>Adds localization resource keys to App_GlobalResources in bulk from a key file.</en>
</lang>

.DESCRIPTION
<lang>
  <zh-CN>键文件每行形如 `键名|中文值|英文值`：中文值写入 lang.zh-cn.resx，英文值同时写入中性 lang.resx 与 lang.en-us.resx，并为中性 designer 追加强类型属性。脚本只做插入，不修改既有条目；BOM 状态按原文件保持；生成的分隔行为空行，避免引入行尾空白。本脚本只读写资源文件，不连接数据库、不启动 IIS、不读取生产配置。</zh-CN>
  <en>Each key-file line has the form `key|chinese value|english value`: the Chinese value goes to lang.zh-cn.resx, the English value goes to both the neutral lang.resx and lang.en-us.resx, and a strongly typed property is appended to the neutral designer. The script only inserts and never rewrites existing entries; BOM state follows each original file; generated separator lines are empty so no trailing whitespace is introduced. It only reads and writes resource files and does not connect to databases, start IIS, or read production configuration.</en>
</lang>

.PARAMETER KeyFile
<lang>
  <zh-CN>键文件路径，每行 `键名|中文值|英文值`。</zh-CN>
  <en>Path to the key file, one `key|chinese value|english value` per line.</en>
</lang>

.PARAMETER ResourceDirectory
<lang>
  <zh-CN>App_GlobalResources 目录路径。</zh-CN>
  <en>Path to the App_GlobalResources directory.</en>
</lang>
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$KeyFile,

    [Parameter(Mandatory = $true)]
    [string]$ResourceDirectory
)

$ErrorActionPreference = 'Stop'

# <lang>
#   <zh-CN>键文件必须存在，否则停止，避免静默写入 0 条却报告成功。</zh-CN>
#   <en>Stop when the key file is missing so an empty write is never reported as success.</en>
# </lang>
if (-not (Test-Path -LiteralPath $KeyFile)) {
    throw "Key file not found: $KeyFile"
}

# <lang>
#   <zh-CN>忽略空行与以 # 开头的注释行（整行注释，含注释文字），避免把说明行当成资源键写入。</zh-CN>
#   <en>Ignore blank lines and lines beginning with # (including commented text) so documentation lines are never written as resource keys.</en>
# </lang>
$lines = [IO.File]::ReadAllLines($KeyFile) | Where-Object {
    $trimmed = $_.Trim()
    $trimmed.Length -gt 0 -and -not $trimmed.StartsWith('#')
}
if ($lines.Count -eq 0) {
    throw "Key file contains no entries: $KeyFile"
}

$zhEntries = ''
$enEntries = ''
$designerProperties = ''

foreach ($line in $lines) {
    # <lang>
    #   <zh-CN>每行必须恰好三段，段数不符时抛出并指出行内容，防止把半行数据写入资源文件。</zh-CN>
    #   <en>Require exactly three segments per line and fail with the offending line so half-valid data never reaches the resource files.</en>
    # </lang>
    $parts = $line.Split('|')
    if ($parts.Count -ne 3) {
        throw "Malformed key line (expected key|zh|en): $line"
    }

    $key = $parts[0].Trim()
    $zh = $parts[1]
    $en = $parts[2]

    $zhEntries += "  <data name=`"$key`" xml:space=`"preserve`">`r`n    <value>$zh</value>`r`n  </data>`r`n"
    $enEntries += "  <data name=`"$key`" xml:space=`"preserve`">`r`n    <value>$en</value>`r`n  </data>`r`n"

    # <lang>
    #   <zh-CN>designer 属性的文档注释沿用既有生成风格：zh-CN 与 en 两行都引用中性（英文）值，与 VS 生成结果一致。</zh-CN>
    #   <en>The designer property documentation follows the existing generated style: both the zh-CN and en comment lines quote the neutral (English) value, matching Visual Studio output.</en>
    # </lang>
    $designerProperties += "`r`n`r`n        /// <summary>`r`n        /// <lang>`r`n        ///   <zh-CN>查找类似以下内容的本地化字符串：$en</zh-CN>`r`n        ///   <en>Looks up a localized string similar to: $en</en>`r`n        /// </lang>`r`n        /// </summary>`r`n        internal static string $key {`r`n            get {`r`n                return ResourceManager.GetString(`"$key`", resourceCulture);`r`n            }`r`n        }"
}

function Add-ResxEntries {
    param([string]$Path, [string]$Entries)

    $bytes = [IO.File]::ReadAllBytes($Path)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $text = [IO.File]::ReadAllText($Path)
    $index = $text.LastIndexOf('</root>')
    if ($index -lt 0) {
        throw "Resource file has no closing root element: $Path"
    }

    [IO.File]::WriteAllText($Path, $text.Insert($index, $Entries), (New-Object System.Text.UTF8Encoding $hasBom))
}

$resxPath = Join-Path $ResourceDirectory 'lang.resx'
$resxEnPath = Join-Path $ResourceDirectory 'lang.en-us.resx'
$resxZhPath = Join-Path $ResourceDirectory 'lang.zh-cn.resx'
$designerPath = Join-Path $ResourceDirectory 'lang.designer.cs'

Add-ResxEntries -Path $resxPath -Entries $enEntries
Add-ResxEntries -Path $resxEnPath -Entries $enEntries
Add-ResxEntries -Path $resxZhPath -Entries $zhEntries

# <lang>
#   <zh-CN>designer 属性插入到最后一个属性的闭合大括号之后，保持既有成员顺序与缩进。</zh-CN>
#   <en>Insert designer properties after the last property's closing brace to preserve existing member order and indentation.</en>
# </lang>
$designerBytes = [IO.File]::ReadAllBytes($designerPath)
$designerBom = ($designerBytes.Length -ge 3 -and $designerBytes[0] -eq 0xEF -and $designerBytes[1] -eq 0xBB -and $designerBytes[2] -eq 0xBF)
$designerText = [IO.File]::ReadAllText($designerPath)
$lastGetString = $designerText.LastIndexOf('resourceCulture);')
if ($lastGetString -lt 0) {
    throw "Designer file has no generated property: $designerPath"
}

$closeIndex = $designerText.IndexOf("`r`n        }", $lastGetString)
if ($closeIndex -lt 0) {
    throw "Designer file property close not found: $designerPath"
}

[IO.File]::WriteAllText(
    $designerPath,
    $designerText.Insert($closeIndex + 11, $designerProperties),
    (New-Object System.Text.UTF8Encoding $designerBom))

Write-Host "Inserted $($lines.Count) resource keys from $([IO.Path]::GetFileName($KeyFile))"

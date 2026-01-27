$filePath = "stimplanaskyrsla_2601261341.xlsx"
$uri = "http://localhost:5094/api/timereports/diagnose"

$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"

$fileBytes = [System.IO.File]::ReadAllBytes($filePath)
$fileName = [System.IO.Path]::GetFileName($filePath)

$bodyLines = @(
    "--$boundary",
    "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"",
    "Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "",
    [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileBytes),
    "--$boundary--"
)

$body = $bodyLines -join $LF
$bodyBytes = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes($body)

$request = [System.Net.HttpWebRequest]::Create($uri)
$request.Method = "POST"
$request.ContentType = "multipart/form-data; boundary=$boundary"
$request.ContentLength = $bodyBytes.Length

$requestStream = $request.GetRequestStream()
$requestStream.Write($bodyBytes, 0, $bodyBytes.Length)
$requestStream.Close()

try {
    $response = $request.GetResponse()
    $responseStream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($responseStream)
    $responseBody = $reader.ReadToEnd()
    $reader.Close()
    $responseStream.Close()
    $response.Close()
    
    $responseBody | Out-File -FilePath "response.json" -Encoding UTF8
    Write-Host "Response saved to response.json"
    Write-Host $responseBody
} catch {
    Write-Host "Error: $_"
    $_.Exception.Response | Format-List
}


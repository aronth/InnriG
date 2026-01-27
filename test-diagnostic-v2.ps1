Add-Type -AssemblyName System.Net.Http

$filePath = "stimplanaskyrsla_2601261341.xlsx"
$uri = "http://localhost:5094/api/timereports/parse-debug"

$fileBytes = [System.IO.File]::ReadAllBytes($filePath)
$fileName = [System.IO.Path]::GetFileName($filePath)

$multipartContent = New-Object System.Net.Http.MultipartFormDataContent
$fileContent = [System.Net.Http.ByteArrayContent]::new($fileBytes)
$fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
$multipartContent.Add($fileContent, "file", $fileName)

$httpClient = New-Object System.Net.Http.HttpClient
try {
    $response = $httpClient.PostAsync($uri, $multipartContent).Result
    $responseBody = $response.Content.ReadAsStringAsync().Result
    
    if ($response.IsSuccessStatusCode) {
        $responseBody | Out-File -FilePath "parse-debug.json" -Encoding UTF8
        Write-Host "Success! Response saved to parse-debug.json"
        try {
            $json = $responseBody | ConvertFrom-Json
            Write-Host "`nResponse preview (first 3 rows):"
            if ($json.Rows) {
                $json.Rows | Select-Object -First 3 | ConvertTo-Json -Depth 10
                Write-Host "`nTotal rows parsed: $($json.Rows.Count)"
            }
            Write-Host "`nColumn map:"
            if ($json.ColumnMap) {
                $json.ColumnMap | ConvertTo-Json
            }
        } catch {
            Write-Host "Error parsing JSON: $_"
            Write-Host "Raw response (first 1000 chars):"
            $responseBody.Substring(0, [Math]::Min(1000, $responseBody.Length))
        }
    } else {
        Write-Host "Error: $($response.StatusCode) - $($response.ReasonPhrase)"
        Write-Host "Response: $responseBody"
    }
} catch {
    Write-Host "Exception: $_"
} finally {
    $httpClient.Dispose()
    $multipartContent.Dispose()
}


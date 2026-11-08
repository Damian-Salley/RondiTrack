param([ValidateSet('up', 'down')][string]$Action = 'up')
$ErrorActionPreference = 'Stop'
$env:COMPOSE_PROJECT_NAME = 'ronditrack-assignment92'
if ($Action -eq 'down') {
    docker compose -f compose.yaml -f compose.e2e.yaml down -v
    if ($LASTEXITCODE -ne 0) { throw 'Test stack cleanup failed.' }
    return
}
foreach ($keyName in @('POSTGRES_PASSWORD', 'E2E_PASSWORD', 'JWT_KEY', 'SESSION_SECRET')) {
    if (-not [Environment]::GetEnvironmentVariable($keyName)) {
        $randomBytes = New-Object byte[] 32
        [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($randomBytes)
        $value = [Convert]::ToBase64String($randomBytes)
        if ($keyName -eq 'SESSION_SECRET') { $value = $value.TrimEnd('=').Replace('+', '-').Replace('/', '_') }
        [Environment]::SetEnvironmentVariable($keyName, $value, 'Process')
    }
}
$env:API_BASE_URL = 'http://localhost:8080'
docker compose -f compose.yaml -f compose.e2e.yaml up -d --build --wait
if ($LASTEXITCODE -ne 0) { throw 'Test stack startup failed.' }

param([string]$Name = "FormaAI")

$password = $env:FORMAAI_DB_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) { throw "Ustaw FORMAAI_DB_PASSWORD przed wykonaniem kopii." }
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$query = "BACKUP DATABASE [$Name] TO DISK = N'/var/opt/mssql/backups/$Name-$stamp.bak' WITH COPY_ONLY, CHECKSUM"
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $password -Q $query

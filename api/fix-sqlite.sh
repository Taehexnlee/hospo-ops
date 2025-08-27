set -euo pipefail

# 루트 확인
[ -f "api.csproj" ] || { echo "api.csproj 없는 위치입니다. ~/hospo-ops/api 에서 실행하세요"; exit 1; }

# EF 래퍼 (로컬 도구 우선)
ef() {
  if [ -f ".config/dotnet-tools.json" ] && grep -q dotnet-ef .config/dotnet-tools.json; then
    dotnet tool run dotnet-ef "$@"
  else
    dotnet ef "$@"
  fi
}

echo "▶ appsettings.Development.json => SQLite 확인/설정"
if ! grep -q 'Data Source=.*\.db' appsettings.Development.json 2>/dev/null; then
  cat > appsettings.Development.json <<'JSON'
{
  "ConnectionStrings": {
    "Default": "Data Source=hospoops.dev.db"
  },
  "Serilog": { "MinimumLevel": "Information" },
  "Square": { "WebhookSignatureKey": "replace-with-your-square-webhook-secret" },
  "AllowedHosts": "*"
}
JSON
  echo "  ↳ 생성 완료"
else
  echo "  ↳ 이미 SQLite 설정 감지"
fi

echo "▶ Program.cs 환경별 DB 분기 확인/패치"
if ! grep -q 'UseSqlite' Program.cs; then
  # 최소 침습 패치: Sqlite(개발) / SqlServer(그 외)
  perl -0777 -pe '
    s/builder\.Services\.AddDbContext<AppDbContext>\([^)]*\);\s*//s;
  ' -i.bak Program.cs
  awk '
    BEGIN{printed=0}
    {print}
    /var cs = builder.Configuration.GetConnectionString\("Default"\);/ && printed==0{
      print "if (builder.Environment.IsDevelopment())";
      print "{";
      print "    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));";
      print "}";
      print "else";
      print "{";
      print "    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));";
      print "}";
      printed=1
    }
  ' Program.cs > Program.cs.new && mv Program.cs.new Program.cs
  # using 보강
  grep -q 'using Microsoft.EntityFrameworkCore;' Program.cs || sed -i '' '1i\
using Microsoft.EntityFrameworkCore;
' Program.cs
  echo "  ↳ 분기 로직 삽입 완료 (백업: Program.cs.bak)"
else
  echo "  ↳ 이미 분기 로직 존재"
fi

echo "▶ AppDbContext.cs NetSales 정밀도 경고 예방 (SQL Server용, SQLite 무해)"
if [ -f Data/AppDbContext.cs ] && ! grep -q 'NetSales).*HasPrecision(18, 2)' Data/AppDbContext.cs; then
  sed -i.bak '/Entity<EodReport>().*ToTable("EodReports")/a\
\        b.Entity<EodReport>().Property(x => x.NetSales).HasPrecision(18, 2);\
' Data/AppDbContext.cs
  echo "  ↳ HasPrecision(18,2) 추가 (백업: Data/AppDbContext.cs.bak)"
else
  echo "  ↳ 이미 설정되어 있거나 파일 없음"
fi

echo "▶ 복원/빌드"
dotnet restore
dotnet build -c Debug

echo "▶ 마이그레이션/DB 업데이트"
if ! ls Migrations/*_InitialCreate.* >/dev/null 2>&1; then
  echo "  ↳ InitialCreate 생성"
  ef migrations add InitialCreate
else
  echo "  ↳ 기존 마이그레이션 감지 (생성 스킵)"
fi
ef database update
echo "✔ SQLite database update 완료"

echo
echo "✅ 이제 실행하세요:  dotnet run"
echo "   • 헬스체크: http://localhost:5047/health   (또는 /health2)"
echo "   • EOD 예시: http://localhost:5047/api/eod/1/2025-08-27"

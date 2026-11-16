#!/usr/bin/env bash
# 클라우드·로컬 공통 검증: 순수 로직(Assets/SushiRun/Core) 테스트. Unity 컴파일은 에디터에서만 확인돼요.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet test tests/SushiRun.Core.Tests --nologo 2>&1 | tail -n 5

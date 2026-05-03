@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"
for /f %%i in ('python -c "import sys; print(f'Python{sys.version_info.major}{sys.version_info.minor}')"') do set PYVER=%%i
set "USER_SITE=%APPDATA%\Python\%PYVER%\site-packages"
if exist "%USER_SITE%" (
  if defined PYTHONPATH (
    set "PYTHONPATH=%USER_SITE%;%PYTHONPATH%"
  ) else (
    set "PYTHONPATH=%USER_SITE%"
  )
)
python -m uvicorn main:app --host 0.0.0.0 --port 8000 --reload

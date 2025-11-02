# Audio Visualizer

Небольшой WPF-визуализатор системного аудио (спектроанализатор) для Windows.

## Preview

![Preview](media/preview.png)

## Возможности
- Захват системного звука (WASAPI loopback)
- FFT-анализ в реальном времени
- Радужная палитра, адаптивное количество столбиков
- Плавная анимация ~60 FPS

## Требования
- Windows 10/11
- .NET 8.0 SDK

## Сборка и запуск
```powershell
cd AudioVisualizer
 dotnet build
 dotnet run --project AudioVisualizer/AudioVisualizer.csproj
```

Сборка самодостаточного дистрибутива:
```powershell
dotnet publish AudioVisualizer/AudioVisualizer.csproj -c Release -r win-x64 --self-contained
```
Исполняемый файл: `AudioVisualizer/bin/Release/net8.0/win-x64/publish/AudioVisualizer.exe`

## Технологии
- WPF
- NAudio (WASAPI)

## Примечания
Визуализация симметрична: низкие частоты в центре, высокие — к краям.

$ErrorActionPreference = "Stop"

$path = ".\MainForm.cs"
$utf8 = [System.Text.UTF8Encoding]::new($false)

$text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

$text = $text -replace 'Text = "Ning.*?archivo cargado",', 'Text = "Ning\u00FAn archivo cargado",'
$text = $text -replace '_titleLabel\.Text = "Ning.*?archivo cargado";', '_titleLabel.Text = "Ning\u00FAn archivo cargado";'

$text = $text -replace 'Text = "Play"', 'Text = "Reproducir"'
$text = $text -replace 'Text = "Pause"', 'Text = "Pausa"'
$text = $text -replace 'Text = "Stop"', 'Text = "Parar"'

$text = $text -replace '_playButton\.Text = "Play";', '_playButton.Text = "Reproducir";'
$text = $text -replace '_playButton\.Text = "Pause";', '_playButton.Text = "Pausa";'
$text = $text -replace '_playButton\.Text = "Stop";', '_playButton.Text = "Parar";'

$text = $text -replace 'Text = ".*?adir carpeta"', 'Text = "A\u00F1adir carpeta"'
$text = $text -replace 'Title = "Abrir .*?"', 'Title = "Abrir m\u00FAsica"'
$text = $text -replace 'Description = ".*?adir carpeta de .*?"', 'Description = "A\u00F1adir carpeta de m\u00FAsica"'

$text = $text -replace 'SetStatus\(added == 1 \? "1 archivo .*?\." : \$"\{added\} archivos .*?\."\);', 'SetStatus(added == 1 ? "1 archivo a\u00F1adido." : $"{added} archivos a\u00F1adidos.");'
$text = $text -replace 'SetStatus\("A.*?ade un archivo primero\."\);', 'SetStatus("A\u00F1ade un archivo primero.");'

$text = $text -replace 'Text = "Listo\..*?circo\.",', 'Text = "Creado por Kot1kX",'
$text = $text -replace 'Text = "Listo\..*?",', 'Text = "Creado por Kot1kX",'

$text = $text -replace 'Text = "Boost seguro"', 'Text = "Sin amplificar"'
$text = $text -replace '"Boost seguro"', '"Sin amplificar"'
$text = $text -replace '\$"Boost x\{boost:0\.##\}"', '$"Amp x{boost:0.##}"'

[System.IO.File]::WriteAllText($path, $text, $utf8)

Write-Host "UI corregida: textos, encoding y botones."
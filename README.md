<p align="center">
  <img src="assets/icon/icon.svg" alt="Icono de Disk Space Analyzer: una carpeta ámbar con una barra de uso coral" width="128" height="128">
</p>

<h1 align="center">Disk Space Analyzer</h1>

<p align="center">
  Descubre qué carpetas y archivos ocupan tu disco, ordenados por tamaño.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4" alt=".NET 10">
  <img src="https://img.shields.io/badge/Avalonia-12-8B44AC" alt="Avalonia 12">
  <img src="https://img.shields.io/badge/plataforma-Windows%20%C2%B7%20Linux%20%C2%B7%20macOS-lightgrey" alt="Plataforma: Windows, Linux y macOS">
  <img src="https://img.shields.io/badge/modo-solo%20lectura-4ADE80" alt="Modo solo lectura">
</p>

<p align="center">
  <a href="#resumen-del-proyecto">Resumen</a> ·
  <a href="#requisitos">Requisitos</a> ·
  <a href="#uso">Uso</a> ·
  <a href="#pruebas">Pruebas</a> ·
  <a href="#estructura">Estructura</a> ·
  <a href="#interfaz">Interfaz</a> ·
  <a href="#iconos">Iconos</a>
</p>

<table>
  <tr>
    <th align="center">Inicio</th>
    <th align="center">Analizando</th>
    <th align="center">Resultado</th>
  </tr>
  <tr>
    <td><img src="assets/readme/screenshots/inicio.png" alt="Pantalla inicial: campo de ruta y botones Examinar y Analizar"></td>
    <td><img src="assets/readme/screenshots/analisis.png" alt="Análisis en curso con barra de avance, contador de archivos y botón Cancelar"></td>
    <td><img src="assets/readme/screenshots/resultado.png" alt="Resultado: tarjetas de resumen y árbol de carpetas con barras de porcentaje por elemento"></td>
  </tr>
</table>

<p align="center">
  <img src="assets/readme/screenshots/resultado-completo.png" alt="Análisis de una unidad completa: tamaño total, número de archivos y carpetas, espacio recuperable señalado en verde y árbol de carpetas ordenado de mayor a menor" width="100%">
</p>

---

Aplicación de escritorio que lista las carpetas y archivos de un directorio junto con su tamaño, ordenados de mayor a menor y con el porcentaje que ocupa cada elemento dentro de su carpeta.

Hecha con **C# / .NET 10** y **Avalonia UI 12**, pensada para crecer: hoy corre en Windows y el mismo código puede ejecutarse en Linux y macOS.

## Resumen del proyecto

**Problema:** el disco se llena y no es evidente qué carpetas o archivos ocupan el espacio. El Explorador de Windows no muestra el tamaño de las carpetas y calcularlo a mano es lento.

**Objetivo:** dado un directorio, mostrar de forma rápida y visual qué contiene y cuánto pesa cada elemento, para decidir qué limpiar o mover.

**Qué hace hoy (demo):**
- Analiza una carpeta elegida por el usuario, de forma recursiva y sin bloquear la ventana.
- Muestra un árbol navegable con el tamaño de cada carpeta y archivo, ordenado de mayor a menor.
- Indica qué porcentaje ocupa cada elemento dentro de su carpeta contenedora.
- Permite cancelar el análisis y muestra el avance mientras se ejecuta.
- Clic derecho sobre una carpeta (o archivo) para abrirla en el explorador de archivos del sistema.
- Marca las carpetas sin permiso de lectura y continúa con el resto.

**Qué no hace todavía:** borrar o mover archivos, analizar en paralelo, graficar (treemap) ni exportar resultados. Ver [Siguientes pasos posibles](#siguientes-pasos-posibles).

**Alcance:** herramienta de **solo lectura**; nunca modifica ni elimina nada del disco analizado.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (ver `global.json`)

## Uso

```bash
dotnet run --project src/DiskSpaceAnalyzer.Desktop
```

Elige una carpeta (escribiendo la ruta o con **Examinar…**) y pulsa **Analizar**. El análisis se puede cancelar en cualquier momento. Las carpetas que no se pueden leer se marcan con ⚠ y el resto del análisis continúa.

## Pruebas

```bash
dotnet test
```

> **Plataformas probadas:** hasta ahora las pruebas automáticas y la aplicación se han ejecutado únicamente en **Windows 10**. En Linux y macOS todavía no se ha probado nada: la lógica (`Core`) es portable, pero falta validar la interfaz y el comportamiento del sistema de archivos (permisos, enlaces simbólicos, apertura del explorador).

## Estructura

```
.
├── DiskSpaceAnalyzer.sln
├── Directory.Build.props        Configuración común (net10.0, nullable, warnings como errores)
├── Directory.Packages.props     Versiones de paquetes NuGet en un único lugar
├── global.json                  Versión del SDK
├── src/
│   ├── DiskSpaceAnalyzer.Core/            Dominio y lógica. Sin dependencias externas ni de UI
│   │   ├── Models/                        FileSystemNode, FileNode, DirectoryNode, ScanResult
│   │   ├── Abstractions/                  IFileSystemReader, IDirectoryScanner
│   │   ├── Scanning/                      DirectoryScanner, ScanProgress
│   │   └── Formatting/                    ISizeFormatter, BinarySizeFormatter
│   ├── DiskSpaceAnalyzer.Infrastructure/  Acceso real al disco
│   │   └── FileSystem/                    PhysicalFileSystemReader (System.IO)
│   └── DiskSpaceAnalyzer.Desktop/         Interfaz Avalonia (MVVM) y raíz de composición
│       ├── Views/                         MainWindow.axaml
│       ├── ViewModels/                    MainWindowViewModel, NodeViewModel y su factoría
│       ├── Services/                      Selector de carpetas (adaptador de UI)
│       └── CompositionRoot.cs             Registro de dependencias (DI)
└── tests/
    └── DiskSpaceAnalyzer.Tests/           xUnit: Core, Infrastructure y Desktop
```

### Dirección de las dependencias

```
Desktop ──► Infrastructure ──► Core
   └─────────────────────────────▲
```

`Core` no conoce a nadie. Todo apunta hacia él.

## Principios SOLID aplicados

| Principio | Dónde se ve |
|---|---|
| **S**ingle responsibility | `DirectoryScanner` solo recorre; `BinarySizeFormatter` solo da formato; `NodeViewModel` solo presenta un nodo; el ViewModel principal solo orquesta. |
| **O**pen/closed | Para otro formato de tamaño (base 1000) se añade una clase que implemente `ISizeFormatter`, sin tocar lo existente. |
| **L**iskov | `FileNode` y `DirectoryNode` se usan como `FileSystemNode` sin comprobar el tipo concreto. |
| **I**nterface segregation | Interfaces pequeñas y específicas: `IFileSystemReader`, `IDirectoryScanner`, `ISizeFormatter`, `IFolderPickerService`. |
| **D**ependency inversion | `Core` depende de `IFileSystemReader`, no de `System.IO`. Las implementaciones se inyectan desde `CompositionRoot`. |

## Patrones de diseño

- **Composite**: `FileSystemNode` / `FileNode` / `DirectoryNode` modelan el árbol de forma uniforme.
- **Strategy**: `ISizeFormatter` permite intercambiar cómo se muestra el tamaño.
- **Adapter**: `PhysicalFileSystemReader` y `AvaloniaFolderPickerService` aíslan a la aplicación de `System.IO` y de Avalonia.
- **Factory**: `INodeViewModelFactory` crea los ViewModels de nodos sin que estos se instancien entre sí.
- **MVVM + Command**: `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).
- **Dependency Injection**: `Microsoft.Extensions.DependencyInjection` con una única raíz de composición.

## Interfaz

Tema oscuro con acento coral, definido en `src/DiskSpaceAnalyzer.Desktop/Styles/Theme.axaml` (paleta, tarjetas, tipografía, botones). La disposición de la ventana principal está en `Views/MainWindow.axaml`.

**Responsive:** Avalonia no tiene *media queries*, así que `MainWindow.axaml.cs` marca la ventana con una clase según su ancho y el XAML decide qué cambia:

| Ancho | Clase | Cambios |
|---|---|---|
| ≥ 900 px | — | Diseño completo; el contenido se centra con un máximo de 1280 px |
| < 900 px | `compact` | Menos espaciado y barra de porcentaje más corta |
| < 600 px | `narrow` | Se oculta el porcentaje, los botones bajan a su propia fila y las tarjetas de resumen pasan a 2 × 2 |

> Al ajustar estilos responsive: un valor escrito directamente en el elemento (`Padding="32"`) gana siempre a un estilo, así que los valores por defecto de lo que cambia con el ancho van en `Window.Styles`, no en el elemento.

## Iconos

Los SVG de `assets/` son la fuente; el `.ico` y los PNG se derivan de ellos.

| Archivo | Uso |
|---|---|
| `assets/icon/icon.svg` | Icono de la aplicación (tamaños ≥ 48 px) |
| `assets/icon/icon-small.svg` | Variante simplificada para 16, 24 y 32 px |
| `assets/icon/icon.ico` | Icono multi-resolución (16–256 px) generado: ventana, barra de tareas y `.exe` |
| `assets/icon/png/` | PNG intermedios generados |
| `assets/readme/banner.svg` | Banner horizontal con fondo propio (se ve igual en el tema claro y el oscuro de GitHub); disponible, hoy el README usa el icono centrado |

**Regenerar el `.ico`** tras editar los SVG, desde la raíz del repo:

```bash
pip install pillow
python assets/icon/build-icon.py
```

El script usa Microsoft Edge o Google Chrome en modo headless solo para rasterizar el SVG, y Pillow para reducir los tamaños y empaquetar el `.ico`. Después, `dotnet build` lo incrusta en el `.exe` (`<ApplicationIcon>`) y como recurso de la ventana (`Icon` en `MainWindow.axaml`).

## Decisiones a tener en cuenta

- **Tamaño** = tamaño lógico del archivo, no el espacio realmente ocupado en disco (clústeres, compresión, archivos de OneDrive solo en la nube).
- **Enlaces simbólicos y puntos de unión** se omiten, para evitar bucles infinitos y contar dos veces el mismo contenido.
- **Rendimiento**: el análisis es secuencial en un hilo de fondo, con avance cada 500 archivos y cancelación. El árbol visual crea los hijos solo al expandir.

## Siguientes pasos posibles

1. Análisis en paralelo por carpeta (el cambio queda dentro de `DirectoryScanner`).
2. Sustituir `TreeView` por un control virtualizado (`TreeDataGrid`) para carpetas con cientos de miles de archivos.
3. Treemap o gráfico de barras de las carpetas más pesadas.
4. Exportar el resultado a CSV.
5. Probar en Linux y macOS (la lógica ya es portable; falta validar la interfaz).
6. Integración continua que ejecute `dotnet build` y `dotnet test`.

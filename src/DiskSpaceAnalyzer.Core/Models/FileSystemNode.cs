namespace DiskSpaceAnalyzer.Core.Models;

/// <summary>
/// Elemento del árbol del sistema de archivos. Es el componente común del patrón Composite:
/// los clientes tratan igual a archivos y carpetas y no necesitan comprobar el tipo concreto.
/// </summary>
public abstract class FileSystemNode
{
    protected FileSystemNode(string name, string fullPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }

    public string FullPath { get; }

    public abstract NodeKind Kind { get; }

    /// <summary>Tamaño lógico en bytes. En una carpeta es la suma de todo su contenido.</summary>
    public abstract long SizeInBytes { get; }

    /// <summary>Elementos contenidos. Vacío para los archivos.</summary>
    public abstract IReadOnlyList<FileSystemNode> Children { get; }

    /// <summary>Indica que no se pudo leer por completo el contenido del elemento (p. ej. permisos).</summary>
    public virtual bool HasAccessErrors => false;
}

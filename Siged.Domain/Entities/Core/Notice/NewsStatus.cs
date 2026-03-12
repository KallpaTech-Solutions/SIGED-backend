namespace Siged.Domain.Entities.Core.Notice
{
    /// <summary>
    /// Representa el estado de publicación de una noticia.
    /// </summary>
    public enum NewsStatus
    {
        // El valor 0 suele ser el valor por defecto (Borrador)
        Draft = 0,

        // El valor 1 indica que ya es visible para los alumnos
        Published = 1,

        // El valor 2 para noticias que ya no deben aparecer pero no se quieren borrar
        Archived = 2
    }
}
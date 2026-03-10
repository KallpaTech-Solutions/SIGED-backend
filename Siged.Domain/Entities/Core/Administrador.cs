namespace Siged.Domain.Entities.Core
{
    // Esta clase representa a todo el personal administrativo (incluyendo SuperAdmins)
    public class Administrador : Persona
    {
        public int? DependenciaId { get; set; }
        public Dependencia? Dependencia { get; set; }
        // Para saber si es personal de planta o externo (auditoría)
        public bool EsPersonalInterno { get; set; } = true;
    }
}

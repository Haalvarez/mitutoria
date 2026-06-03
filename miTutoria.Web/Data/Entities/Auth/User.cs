using miTutoria.Web.Data.Entities.Academic;

namespace miTutoria.Web.Data.Entities.Auth;

public enum UserRole { Parent, Student }

public enum SchoolLevel { Primario, Secundario }

public enum Gender { NoEspecificado, Femenino, Masculino }

public enum ExplanationLevel { AcordeAlAño, UnPocoBasico, BastanteBasico }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public UserRole Role { get; set; }
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;

    // Login del alumno (generado por el padre)
    public string? StudentUsername { get; set; }

    // Perfil estudiante
    public SchoolLevel SchoolLevel { get; set; }
    public int? Grade { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool HasAdhd { get; set; }
    public Gender Gender { get; set; } = Gender.NoEspecificado;

    // Intereses personales — para que el tutor conecte (fútbol, manga, etc.)
    public string? Interests { get; set; }

    // Estilo de aprendizaje
    public bool PrefShortMessages { get; set; }
    public bool PrefVisualExamples { get; set; }
    public bool PrefFrequentPraise { get; set; }
    public bool PrefExtraPatience { get; set; }
    public bool PrefSlowPace { get; set; }

    // Solo TDAH
    public ExplanationLevel ExplanationLevel { get; set; } = ExplanationLevel.AcordeAlAño;
    public bool PrefOneQuestionOnly { get; set; }
    public bool PrefRefocusReminder { get; set; }

    public ICollection<Classroom> Classrooms { get; set; } = new List<Classroom>();
}

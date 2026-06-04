using miTutoria.Web.Inbox;
using Xunit;

namespace miTutoria.Tests;

public class ClassroomMailParserTests
{
    // Fixture real (reconstruido del original que lee el Apps Script): "Nueva tarea".
    private const string SilabasBody = @"[image: Logotipo de Classroom]
Ajustes de notificaciones
<https://accounts.google.com/AccountChooser?continue=https://classroom.google.com/s>
1ro A Prácticas del Lenguaje 2026
<https://accounts.google.com/AccountChooser?continue=https://classroom.google.com/c/NzkzOTI3NDg3MTc5>
Nueva tarea

Las sílabas
Leer y realizar las actividades de las páginas 150 a 153.
Ver detalles
<https://accounts.google.com/AccountChooser?continue=https://classroom.google.com/c/NzkzOTI3NDg3MTc5/a/ODU0Mjk2MzY0NTAw/details>
Publicado el 6:56 a. m., may 7 (GMT-03:00) por Lucas Acosta

[image: Logotipo de Google]
Google LLC, 1600 Amphitheatre Parkway, Mountain View, CA 94043, EE. UU";

    // Fixture real: "Fecha de entrega mañana" con fecha y descripción.
    private const string AdolescenciaBody = @"[image: Logotipo de Classroom]
Ajustes de notificaciones
<https://accounts.google.com/AccountChooser?continue=https://classroom.google.com/s>
1ro A Construcción de la Ciudadanía 2026
<https://accounts.google.com/AccountChooser?continue=https://classroom.google.com/c/NzkzOTI3NTc2ODk3>
Se entrega mañana

Adolescencia
Hola chicos!
Les envío el PPT que vimos sobre la adolescencia.
Fecha de entrega: 28 may
Ver tarea
<https://accounts.google.com/AccountChooser?continue=https://classroom.google.com/c/NzkzOTI3NTc2ODk3/a/NzkzOTI3OTI1OTIx/submissions>

[image: Logotipo de Google]
Google LLC, 1600 Amphitheatre Parkway, Mountain View, CA 94043, EE. UU";

    [Fact]
    public void ParseaTarea_SinFecha()
    {
        var r = ClassroomMailParser.Parse(
            "Nueva tarea: \"Las sílabas\"",
            "Lucas Acosta (Classroom) <no-reply@classroom.google.com>",
            "dariaalvarezblardoni@colegiosanramon.edu.ar",
            SilabasBody);

        Assert.NotNull(r);
        Assert.Equal(ClassroomItemType.Assignment, r!.Type);
        Assert.Equal("Las sílabas", r.Title);
        Assert.Equal("dariaalvarezblardoni@colegiosanramon.edu.ar", r.StudentEmail);
        Assert.Equal("1ro A Prácticas del Lenguaje 2026", r.CourseRaw);
        Assert.Equal("Prácticas del Lenguaje", r.CourseNormalized);
        Assert.Equal("Lucas Acosta", r.Teacher);
        Assert.Null(r.DueDateRaw);
        Assert.Equal("NzkzOTI3NDg3MTc5", r.CourseId);
        Assert.Equal("ODU0Mjk2MzY0NTAw", r.ItemId);
        Assert.Equal("Leer y realizar las actividades de las páginas 150 a 153.", r.Description);
    }

    [Fact]
    public void ParseaEntregaManana_ConFecha()
    {
        var r = ClassroomMailParser.Parse(
            "Fecha de entrega mañana: \"Adolescencia\"",
            "Maria Alejandra Dapiaggi (Classroom) <no-reply@classroom.google.com>",
            "dariaalvarezblardoni@colegiosanramon.edu.ar",
            AdolescenciaBody);

        Assert.NotNull(r);
        Assert.Equal(ClassroomItemType.DueReminder, r!.Type);
        Assert.Equal("Adolescencia", r.Title);
        Assert.Equal("Construcción de la Ciudadanía", r.CourseNormalized);
        Assert.Equal("Maria Alejandra Dapiaggi", r.Teacher);
        Assert.Equal("28 may", r.DueDateRaw);
        Assert.Equal("NzkzOTI3NTc2ODk3", r.CourseId);
        Assert.Equal("NzkzOTI3OTI1OTIx", r.ItemId);
    }

    [Fact]
    public void ToleraReenviado_ConPrefijoFwd()
    {
        var r = ClassroomMailParser.Parse(
            "Fwd: Nuevo material: \"VIDEO ECUACIONES\"",
            "Ezequiel Letto (Classroom) <no-reply@classroom.google.com>",
            "dariaalvarezblardoni@colegiosanramon.edu.ar",
            "Ajustes de notificaciones\n1ro B Matemática 2026\nNuevo material\n\nVIDEO ECUACIONES");

        Assert.NotNull(r);
        Assert.Equal(ClassroomItemType.Material, r!.Type);
        Assert.Equal("VIDEO ECUACIONES", r.Title);
        Assert.Equal("Matemática", r.CourseNormalized);
    }
}

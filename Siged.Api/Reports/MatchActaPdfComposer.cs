using System.Globalization;
using System.Net.Http;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Siged.Api.Controllers.Core.Tournaments;

namespace Siged.Api.Reports;

public static class MatchActaPdfComposer
{
    public static byte[] Generate(MatchReportResponse report)
    {
        var leftLogo = TryDownloadImage(report.LeftLogoUrl);
        var rightLogo = TryDownloadImage(report.RightLogoUrl);
        var culture = CultureInfo.GetCultureInfo("es-PE");
        var when = report.ScheduledAt.Year >= 1900
            ? report.ScheduledAt.ToString("dddd d 'de' MMMM 'de' yyyy HH:mm", culture)
            : "Sin fecha programada";
        var championshipLine = report.TournamentName ?? "—";
        if (!string.IsNullOrWhiteSpace(report.CompetitionName))
            championshipLine = $"{championshipLine} · {report.CompetitionName}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(26);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().ShowOnce().Column(header =>
                {
                    header.Spacing(4);
                    header.Item().Background(Colors.BlueGrey.Darken4).Padding(10).Row(row =>
                    {
                        row.ConstantItem(68).AlignMiddle().AlignCenter().Height(52).Element(c =>
                        {
                            if (leftLogo != null)
                                c.Image(leftLogo).FitArea();
                            else
                                c.Text("");
                        });
                        row.RelativeItem().Column(top =>
                        {
                            top.Item().AlignCenter().Text("SIGED — ACTA DE PARTIDO")
                                .Bold().FontSize(11.5f).FontColor(Colors.Grey.Lighten3);
                            top.Item().PaddingTop(2).AlignCenter().Text(championshipLine)
                                .SemiBold().FontSize(12.5f).FontColor(Colors.White);
                            top.Item().PaddingTop(1).AlignCenter().Text(
                                    string.IsNullOrWhiteSpace(report.DisciplineName) ? "—" : report.DisciplineName!)
                                .SemiBold().FontSize(10).FontColor("#a7f3d0");
                            top.Item().PaddingTop(6).Row(scoreRow =>
                            {
                                scoreRow.RelativeItem().AlignMiddle().AlignRight().PaddingRight(5)
                                    .Text(report.LocalTeamName ?? "Local")
                                    .SemiBold().FontSize(9.5f).FontColor(Colors.White);
                                scoreRow.ConstantItem(42).AlignMiddle()
                                    .Background("#0f766e").PaddingVertical(5).AlignCenter()
                                    .Text($"{report.LocalScore}").Bold().FontSize(15).FontColor("#ecfdf5");
                                scoreRow.ConstantItem(20).AlignMiddle().AlignCenter()
                                    .Text("—").FontColor(Colors.Grey.Lighten1).FontSize(11);
                                scoreRow.ConstantItem(42).AlignMiddle()
                                    .Background("#0f766e").PaddingVertical(5).AlignCenter()
                                    .Text($"{report.VisitorScore}").Bold().FontSize(15).FontColor("#ecfdf5");
                                scoreRow.RelativeItem().AlignMiddle().AlignLeft().PaddingLeft(5)
                                    .Text(report.VisitorTeamName ?? "Visitante")
                                    .SemiBold().FontSize(9.5f).FontColor(Colors.White);
                            });
                        });
                        row.ConstantItem(68).AlignMiddle().AlignCenter().Height(52).Element(c =>
                        {
                            if (rightLogo != null)
                                c.Image(rightLogo).FitArea();
                            else
                                c.Text("");
                        });
                    });
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    content.Spacing(10);

                    content.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten5).Padding(10).Column(meta =>
                        {
                            meta.Spacing(4);
                            MetaLine(meta, "Fecha / hora", when);
                            if (!string.IsNullOrWhiteSpace(report.VenueName))
                                MetaLine(meta, "Sede", report.VenueName!);
                            MetaLine(meta, "Estado", report.StatusLabel);
                            MetaLine(meta, "Definicion", report.DecisionType);
                            if (report.LocalPenaltyScore > 0 || report.VisitorPenaltyScore > 0)
                                MetaLine(meta, "Penales", $"{report.LocalPenaltyScore} - {report.VisitorPenaltyScore}");
                            if (!string.IsNullOrWhiteSpace(report.MatchNote))
                                MetaLine(meta, "Nota", report.MatchNote!);
                        });

                    foreach (var team in report.Teams)
                    {
                        content.Item().PaddingTop(2).Text(team.TeamName)
                            .Bold().FontSize(10.5f).FontColor(Colors.BlueGrey.Darken3);
                        content.Item().Element(c => RenderTeamTable(c, team));
                    }

                    if (report.Timeline.Count > 0)
                    {
                        content.Item().PaddingTop(4).Text("Cronologia del partido")
                            .Bold().FontSize(10.5f).FontColor(Colors.BlueGrey.Darken3);
                        content.Item().Element(c => RenderTimelineTable(c, report.Timeline));
                    }
                });

                page.Footer().PaddingTop(5).AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                    t.Span("SIGED · Pagina ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void MetaLine(ColumnDescriptor meta, string label, string value)
    {
        meta.Item().Text(t =>
        {
            t.Span(label + ": ").SemiBold().FontColor(Colors.Grey.Darken3);
            t.Span(value).FontColor(Colors.Grey.Darken4);
        });
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.Background(Colors.BlueGrey.Lighten4)
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5).PaddingHorizontal(4)
            .DefaultTextStyle(x => x.SemiBold().FontSize(8));

    private static IContainer BodyCell(IContainer c, bool shaded = false) =>
        c.Background(shaded ? Colors.Grey.Lighten5 : Colors.White)
            .BorderBottom(0.6f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4).PaddingHorizontal(4);

    private static void RenderTeamTable(IContainer container, MatchReportTeamResponse team)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(26);
                cols.RelativeColumn(2.8f);
                cols.ConstantColumn(62);
                cols.ConstantColumn(24);
                cols.ConstantColumn(24);
                cols.ConstantColumn(24);
                cols.ConstantColumn(24);
                cols.ConstantColumn(24);
                cols.ConstantColumn(28);
                cols.ConstantColumn(28);
                cols.RelativeColumn(1.35f);
            });

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("Nro");
                h.Cell().Element(HeaderCell).Text("Jugador");
                h.Cell().Element(HeaderCell).Text("Condicion");
                h.Cell().Element(HeaderCell).AlignRight().Text("Gol");
                h.Cell().Element(HeaderCell).AlignRight().Text("TA");
                h.Cell().Element(HeaderCell).AlignRight().Text("2A");
                h.Cell().Element(HeaderCell).AlignRight().Text("TR");
                h.Cell().Element(HeaderCell).AlignRight().Text("R2A");
                h.Cell().Element(HeaderCell).AlignRight().Text("Sale");
                h.Cell().Element(HeaderCell).AlignRight().Text("Entra");
                h.Cell().Element(HeaderCell).Text("Observacion");
            });

            for (var i = 0; i < team.Players.Count; i++)
            {
                var p = team.Players[i];
                var shaded = i % 2 == 1;
                table.Cell().Element(x => BodyCell(x, shaded)).Text(p.Number?.ToString() ?? "-");
                table.Cell().Element(x => BodyCell(x, shaded)).Text(p.PlayerName);
                table.Cell().Element(x => BodyCell(x, shaded)).Text(p.Role);
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.Goals));
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.YellowCards));
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.SecondYellowCards));
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.DirectRedCards));
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.DoubleYellowRedCards));
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.SubstitutionsOut));
                table.Cell().Element(x => BodyCell(x, shaded)).AlignRight().Text(AsStat(p.SubstitutionsIn));
                table.Cell().Element(x => BodyCell(x, shaded)).Text(string.IsNullOrWhiteSpace(p.Observation) ? "-" : p.Observation!);
            }

            table.Cell().ColumnSpan(3).Element(HeaderCell).Text($"Resumen · Titulares {team.StartersCount} · Suplentes {team.SubstitutesCount}");
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalGoals.ToString());
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalYellowCards.ToString());
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalSecondYellowCards.ToString());
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalDirectRedCards.ToString());
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalDoubleYellowRedCards.ToString());
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalSubstitutionsOut.ToString());
            table.Cell().Element(HeaderCell).AlignRight().Text(team.TotalSubstitutionsIn.ToString());
            table.Cell().Element(HeaderCell).Text(" ");
        });
    }

    private static void RenderTimelineTable(IContainer container, IReadOnlyList<MatchReportEventLine> lines)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(32);
                cols.ConstantColumn(32);
                cols.ConstantColumn(74);
                cols.RelativeColumn(1.2f);
                cols.RelativeColumn(2.4f);
            });

            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("Per");
                h.Cell().Element(HeaderCell).Text("Min");
                h.Cell().Element(HeaderCell).Text("Tipo");
                h.Cell().Element(HeaderCell).Text("Equipo");
                h.Cell().Element(HeaderCell).Text("Detalle");
            });

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var shaded = i % 2 == 1;
                table.Cell().Element(x => BodyCell(x, shaded)).Text(line.Period > 0 ? line.Period.ToString() : "-");
                table.Cell().Element(x => BodyCell(x, shaded)).Text(line.Minute >= 0 ? line.Minute.ToString() : "-");
                table.Cell().Element(x => BodyCell(x, shaded)).Text(line.Category);
                table.Cell().Element(x => BodyCell(x, shaded)).Text(string.IsNullOrWhiteSpace(line.TeamName) ? "-" : line.TeamName!);
                table.Cell().Element(x => BodyCell(x, shaded)).Text(line.Text);
            }
        });
    }

    private static string AsStat(int value) => value == 0 ? "-" : value.ToString();

    private static byte[]? TryDownloadImage(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        try
        {
            using var http = new HttpClient();
            return http.GetByteArrayAsync(url).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}

using InnriGreifi.API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InnriGreifi.API.Services;

public interface IGiftCardPdfService
{
    byte[] GeneratePdf(GiftCard giftCard, bool includeBackground = false);
}

public class GiftCardPdfService : IGiftCardPdfService
{
    public GiftCardPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(GiftCard giftCard, bool includeBackground = false)
    {
        // A5 size: 148mm x 210mm
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(20);

                page.Content()
                    .Column(column =>
                    {
                        // Background (if enabled)
                        if (includeBackground)
                        {
                            column.Item()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(10)
                                .Border(2)
                                .BorderColor(Colors.Grey.Darken1);
                        }

                        // Header
                        column.Item()
                            .AlignCenter()
                            .PaddingBottom(20)
                            .Text("Gjafakort")
                            .FontSize(32)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        // Gift Card Number
                        column.Item()
                            .AlignCenter()
                            .PaddingBottom(15)
                            .Text($"Númer: {giftCard.Number}")
                            .FontSize(18)
                            .Bold();

                        // Amount
                        column.Item()
                            .AlignCenter()
                            .PaddingBottom(20)
                            .Text($"Upphæð: {FormatCurrency(giftCard.Amount)}")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Green.Darken2);

                        // Message (if provided)
                        if (!string.IsNullOrWhiteSpace(giftCard.Message))
                        {
                            column.Item()
                                .AlignCenter()
                                .PaddingBottom(15)
                                .Text(giftCard.Message)
                                .FontSize(14)
                                .Italic();
                        }

                        // Template name (if exists)
                        if (giftCard.Template != null)
                        {
                            column.Item()
                                .AlignCenter()
                                .PaddingBottom(10)
                                .Text($"Tegund: {giftCard.Template.Name}")
                                .FontSize(12);
                        }

                        // Expiration Date
                        if (giftCard.ExpirationDate.HasValue)
                        {
                            column.Item()
                                .AlignCenter()
                                .PaddingBottom(10)
                                .Text($"Gildir til: {giftCard.ExpirationDate.Value:dd.MM.yyyy}")
                                .FontSize(12);
                        }
                        else
                        {
                            column.Item()
                                .AlignCenter()
                                .PaddingBottom(10)
                                .Text("Gildir til: _______________")
                                .FontSize(12)
                                .Bold();
                        }

                        // DK Number
                        if (!string.IsNullOrWhiteSpace(giftCard.DkNumber))
                        {
                            column.Item()
                                .AlignCenter()
                                .PaddingTop(10)
                                .Text($"DK númer: {giftCard.DkNumber}")
                                .FontSize(12)
                                .Bold();
                        }
                        else
                        {
                            column.Item()
                                .AlignCenter()
                                .PaddingTop(10)
                                .Text("DK númer: _______________")
                                .FontSize(12)
                                .Bold();
                        }

                        // Status
                        column.Item()
                            .AlignCenter()
                            .PaddingTop(20)
                            .Text($"Staða: {GetStatusText(giftCard.Status)}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
            });
        });

        return document.GeneratePdf();
    }

    private string FormatCurrency(decimal amount)
    {
        // Icelandic format: 5.000 kr.
        var formatted = amount.ToString("N0", new System.Globalization.CultureInfo("is-IS"));
        return $"{formatted} kr.";
    }

    private string GetStatusText(GiftCardStatus status)
    {
        return status switch
        {
            GiftCardStatus.Created => "Búið til",
            GiftCardStatus.Sold => "Selt",
            GiftCardStatus.Redeemed => "Notað",
            GiftCardStatus.Expired => "Útrunnið",
            _ => status.ToString()
        };
    }
}




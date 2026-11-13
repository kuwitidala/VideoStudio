namespace VideoStudio.Models;

// Данные для формы записи: услуги и свободные слоты из БД
public class BookingFormViewModel
{
    public List<Service> Services { get; set; } = new();

    public List<Schedule> Slots { get; set; } = new();

    public int? SelectedServiceId { get; set; }
}

// Данные для главной страницы
public class HomeViewModel
{
    public List<Service> Services { get; set; } = new();

    public List<PortfolioItem> Works { get; set; } = new();

    public List<Review> Reviews { get; set; } = new();

    public BookingFormViewModel Booking { get; set; } = new();
}

// Картинка или видео (для partial-представления _Media)
public record MediaViewModel(string? Url, string? Alt, string? CssClass = null);

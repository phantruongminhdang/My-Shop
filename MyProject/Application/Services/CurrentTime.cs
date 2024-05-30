using Application.Interfaces.Services;

namespace Application.Services
{
    public class CurrentTime : ICurrentTime
    {
        public DateTime GetCurrentTime() => DateTime.Now;
    }
}

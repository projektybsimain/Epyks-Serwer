
namespace Epyks_Serwer
{
    public class Command
    {
        public string Text { get; private set; }
        public int ParametersCount { get; private set; }

        public Command(string text)
        {
            Text = text;
            ParametersCount = 0;
        }

        public Command(string text, int parametersCount)
        {
            Text = text;
            ParametersCount = parametersCount;
        }

        public static bool operator ==(Command first, Command second)
        {
            return (first.Text == second.Text) && (first.ParametersCount == second.ParametersCount);
        }

        public static bool operator !=(Command first, Command second)
        {
            return !((first.Text == second.Text) && (first.ParametersCount == second.ParametersCount));
        }
    }
}

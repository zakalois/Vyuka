using System;
using System.Threading.Tasks;

namespace Vyuka.Services
{
    public class LessonEmailBuilder
    {
        public Task<string> BuildPlannedAsync(
            string studentName,
            string subject,
            string topic,
            DateTime date,
            TimeSpan start,
            string meetLink)
        {
            string html = $@"
<!DOCTYPE html>
<html lang='cs'>
<body style='font-family: Arial, sans-serif; background: #f5f5f5; padding: 20px; margin: 0;'>

    <div style='max-width: 600px; margin: auto; background: white; padding: 25px; border-radius: 8px; border: 1px solid #ddd;'>

        <!-- HEADER S LOGEM -->
        <table width='100%' style='margin-bottom:25px;'>
            <tr>
                <td style='width:80px;'>
                    <img src='https://raw.githubusercontent.com/zakalois/Vyuka/master/wwwroot/images/logo.jpg'
                         alt='Logo'
                         width='70'
                         style='display:block; border-radius:6px;'>
                </td>
                <td style='text-align:left; vertical-align:middle;'>
                    <div style='font-size:20px; font-weight:bold; color:#333;'>
                        Výuka – Oznámení lekce
                    </div>
                    <div style='font-size:13px; color:#777;'>
                        Automatický informační systém
                    </div>
                </td>
            </tr>
        </table>

        <h1 style='color:#333; font-size:22px; margin-bottom:15px;'>Nová lekce byla naplánována</h1>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            Dobrý den <strong>{studentName}</strong>,
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            byla pro vás naplánována nová lekce:
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            <strong>Předmět:</strong> {subject}<br>
            <strong>Téma hodiny:</strong> {topic}<br>
            <strong>Datum:</strong> {date:dd.MM.yyyy}<br>
            <strong>Čas začátku hodiny:</strong> {start.ToString(@"hh\:mm")}<br>
        </p>

        <!-- TLAČÍTKO MEET -->
        <div style='text-align:center; margin:30px 0;'>
            <a href='{meetLink}'
               style='background-color:#1a73e8; color:white; padding:12px 20px;
                      text-decoration:none; border-radius:6px; font-size:16px; display:inline-block;'>
                Připojit se na lekci
            </a>
        </div>

        <!-- ZÁLOŽNÍ ODKAZ -->
        <p style='font-size:15px; color:#444; line-height:1.6;'>
            Pokud tlačítko nefunguje, můžete použít tento odkaz:<br>
            <a href='{meetLink}' style='color:#1a73e8;'>{meetLink}</a>
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            Pokud máte jakékoli dotazy, neváhejte mě kontaktovat.
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            S pozdravem,<br><br>
            Ing. Alois Žák<br>
            <a href='tel:+420601172322' style='color:#1a73e8;'>+420 601 172 322</a><br>
            <a href='https://ucitelzak.eu/' style='color:#1a73e8;'>https://ucitelzak.eu/</a>
        </p>

        <div style='margin-top:25px; font-size:12px; color:#777; text-align:center;'>
            Tento e‑mail byl vygenerován automaticky systémem Výuka.
        </div>

    </div>

</body>
</html>";

            return Task.FromResult(html);
        }

        public Task<string> BuildCanceledAsync(
            string studentName,
            string subject,
            string topic,
            DateTime date,
            TimeSpan start,
            TimeSpan end)
        {
            string html = $@"
<!DOCTYPE html>
<html lang='cs'>
<body style='font-family: Arial, sans-serif; background: #f5f5f5; padding: 20px; margin: 0;'>

    <div style='max-width: 600px; margin: auto; background: white; padding: 25px; border-radius: 8px; border: 1px solid #ddd;'>

        <!-- HEADER S LOGEM -->
        <table width='100%' style='margin-bottom:25px;'>
            <tr>
                <td style='width:80px;'>
                    <img src='https://raw.githubusercontent.com/zakalois/Vyuka/master/wwwroot/images/logo.jpg'
                         alt='Logo'
                         width='70'
                         style='display:block; border-radius:6px;'>
                </td>
                <td style='text-align:left; vertical-align:middle;'>
                    <div style='font-size:20px; font-weight:bold; color:#333;'>
                        Výuka – Zrušení lekce
                    </div>
                    <div style='font-size:13px; color:#777;'>
                        Automatický informační systém
                    </div>
                </td>
            </tr>
        </table>

        <h1 style='color:#c62828; font-size:22px; margin-bottom:15px;'>Lekce byla zrušena</h1>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            Dobrý den <strong>{studentName}</strong>,
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            plánovaná lekce byla zrušena:
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            <strong>Předmět:</strong> {subject}<br>
            <strong>Téma hodiny:</strong> {topic}<br>
            <strong>Datum:</strong> {date:dd.MM.yyyy}<br>
            <strong>Čas:</strong> {start.ToString(@"hh\:mm")} – {end.ToString(@"hh\:mm")}<br>
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            Pokud máte jakékoli dotazy, neváhejte mě kontaktovat.
        </p>

        <p style='font-size:15px; color:#444; line-height:1.6;'>
            S pozdravem,<br><br>
            Ing. Alois Žák<br>
            <a href='tel:+420601172322' style='color:#1a73e8;'>+420 601 172 322</a><br>
            <a href='https://ucitelzak.eu/' style='color:#1a73e8;'>https://ucitelzak.eu/</a>
        </p>

        <div style='margin-top:25px; font-size:12px; color:#777; text-align:center;'>
            Tento e‑mail byl vygenerován automaticky systémem Výuka.
        </div>

    </div>

</body>
</html>";

            return Task.FromResult(html);
        }
    }
}

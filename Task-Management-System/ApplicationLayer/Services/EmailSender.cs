using Contract.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Application.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var fromAddress = new MailAddress("giftcardmessenger@gmail.com");
            var toAddress = new MailAddress(toEmail);
            const string password = "gkvc ivgx eglv gnlf";

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromAddress.Address, password),
                EnableSsl = true
            };

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Password Reset Code",
                IsBodyHtml = true,
                Body = $@"
            <h3>Password Reset</h3>
            <p>Sizin təsdiq kodunuz:</p>
            <h2 style='letter-spacing:3px'>{otp}</h2>
            <p>Kod 5 dəqiqə ərzində etibarlıdır.</p>
        "
            };

            await smtp.SendMailAsync(message);
        }

        public async Task SendOverdueTasksEmailAsync(string toEmail, List<Contract.DTOs.OverdueTaskEmailItem> overdueTasks)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || overdueTasks == null || overdueTasks.Count == 0)
                return;

            var fromAddress = new MailAddress("giftcardmessenger@gmail.com", "TMS Bildiriş Sistemi");
            var toAddress = new MailAddress(toEmail);
            const string password = "gkvc ivgx eglv gnlf";

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromAddress.Address, password),
                EnableSsl = true
            };

            var sb = new StringBuilder();
            sb.Append(@"
                <div style='font-family: Arial, sans-serif; color: #333; max-width: 900px; margin: 0 auto;'>
                    <h2 style='color: #d9534f; border-bottom: 2px solid #d9534f; padding-bottom: 10px;'>
                        ⚠️ Vaxtı Keçmiş Tapşırıqlar Haqqında Bildiriş
                    </h2>
                    <p>Hörmətli rəhbərlik, sistemdə vaxtı (deadline) keçmiş və hələ tamamlanmamış tapşırıqların siyahısı aşağıdakı kimidir:</p>
                    <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                        <thead>
                            <tr style='background-color: #f2f2f2; text-align: left;'>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Tapşırıq</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>İcraçı</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Şöbə</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Layihə</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Mərhələ</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Prioritet</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Deadline</th>
                                <th style='padding: 10px; border: 1px solid #ddd;'>Gecikmə (gün)</th>
                            </tr>
                        </thead>
                        <tbody>");

            foreach (var task in overdueTasks)
            {
                var priorityColor = task.Priority switch
                {
                    "Urgent" => "#d9534f",
                    "High" => "#f0ad4e",
                    "Normal" => "#0275d8",
                    _ => "#5cb85c"
                };

                sb.Append($@"
                            <tr>
                                <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold;'>{task.Title}</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{task.AssignedToUserName ?? task.AssignedToEmail ?? "Təyin edilməyib"}</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{task.DivisionName ?? "-"}</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{task.ProjectName ?? "-"}</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{task.LevelName ?? "-"}</td>
                                <td style='padding: 8px; border: 1px solid #ddd; color: {priorityColor}; font-weight: bold;'>{task.Priority}</td>
                                <td style='padding: 8px; border: 1px solid #ddd;'>{task.Deadline:yyyy-MM-dd HH:mm}</td>
                                <td style='padding: 8px; border: 1px solid #ddd; color: #d9534f; font-weight: bold;'>{task.DaysOverdue} gün</td>
                            </tr>");
            }

            sb.Append(@"
                        </tbody>
                    </table>
                    <p style='margin-top: 20px; font-size: 12px; color: #777;'>
                        Bu avtomatik bildiriş Task Management System tərəfindən göndərilmişdir.
                    </p>
                </div>");

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = $"[TMS] Gecikmiş Tapşırıqlar Xəbərdarlığı ({overdueTasks.Count} tapşırıq)",
                IsBodyHtml = true,
                Body = sb.ToString()
            };

            await smtp.SendMailAsync(message);
        }
    }
}

using Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IEmailSender
    {
        Task SendOtpEmailAsync(string toEmail, string otp);
        Task SendOverdueTasksEmailAsync(string toEmail, List<OverdueTaskEmailItem> overdueTasks);
    }
}

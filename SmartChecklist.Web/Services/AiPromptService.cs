namespace SmartChecklist.Web.Services
{
    public class AiPromptService
    {
        public string BuildTeamMemberReminderPrompt(
            string userName,
            int overdueTasks,
            int dueSoonTasks,
            string? topTaskTitle,
            double completionRate)
        {
            return $@"
Bạn là trợ lý công việc thân thiện cho hệ thống quản lý checklist dự án.

Nhiệm vụ của bạn:
Viết một lời nhắc ngắn, tự nhiên, mang tính động viên.

Yêu cầu:
- Tối đa 3 câu
- Văn phong thân thiện, tích cực
- Không dùng gạch đầu dòng
- Không mang tính robot
- Cá nhân hóa theo người dùng

Thông tin:
- Tên: {userName}
- Việc quá hạn: {overdueTasks}
- Việc sắp đến hạn: {dueSoonTasks}
- Việc quan trọng nhất: {topTaskTitle ?? "Không có"}
- Tiến độ hoàn thành: {completionRate:0.#}%

Chỉ trả về nội dung lời nhắc.
";
        }

        public string BuildManagerSummaryPrompt(
            int totalProjects,
            int totalTasks,
            int overdueTasks,
            double completionRate,
            string? mostRiskyChecklist,
            string? overloadedMember)
        {
            return $@"
Bạn là trợ lý quản lý dự án chuyên nghiệp.

Nhiệm vụ:
Viết một đoạn tóm tắt ngắn cho Project Manager về tình hình hiện tại.

Yêu cầu:
- 3 đến 5 câu
- Ngắn gọn, rõ ràng
- Nêu vấn đề quan trọng nhất
- Có 1 đề xuất hành động cụ thể
- Không lan man

Dữ liệu:
- Tổng dự án: {totalProjects}
- Tổng công việc: {totalTasks}
- Công việc quá hạn: {overdueTasks}
- Tỷ lệ hoàn thành: {completionRate:0.#}%
- Checklist rủi ro nhất: {mostRiskyChecklist ?? "Chưa xác định"}
- Thành viên quá tải: {overloadedMember ?? "Chưa xác định"}

Chỉ trả về đoạn tóm tắt, không giải thích thêm.
";
        }
    }
}
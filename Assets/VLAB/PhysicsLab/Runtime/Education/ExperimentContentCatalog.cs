using System;
using System.Collections.Generic;
using System.Linq;

namespace VLAB.PhysicsLab.Education
{
    public sealed class ExperimentLearningContent
    {
        public ExperimentLearningContent(
            string id,
            string title,
            string objective,
            string observationPrompt,
            string setupInstruction,
            string actionInstruction,
            string analysisPrompt,
            string takeaway,
            params string[] guidancePrompts)
        {
            Id = id;
            Title = title;
            Objective = objective;
            ObservationPrompt = observationPrompt;
            SetupInstruction = setupInstruction;
            ActionInstruction = actionInstruction;
            AnalysisPrompt = analysisPrompt;
            Takeaway = takeaway;
            GuidancePrompts = guidancePrompts ?? Array.Empty<string>();
        }

        public string Id { get; }
        public string Title { get; }
        public string Objective { get; }
        public string ObservationPrompt { get; }
        public string SetupInstruction { get; }
        public string ActionInstruction { get; }
        public string AnalysisPrompt { get; }
        public string Takeaway { get; }
        public IReadOnlyList<string> GuidancePrompts { get; }
    }

    public static class ExperimentContentCatalog
    {
        public static IReadOnlyList<ExperimentLearningContent> All { get; } = new[]
        {
            new ExperimentLearningContent(
                "PHY_01", "CON LẮC ĐƠN",
                "Đo chu kỳ và kiểm tra chu kỳ thay đổi thế nào khi chiều dài dây thay đổi.",
                "Quan sát vị trí cân bằng, chiều dài dây và biên độ lệch ban đầu.",
                "Đặt chiều dài dây, giữ biên độ nhỏ rồi chuẩn bị đồng hồ.",
                "Thả vật không đẩy và đo thời gian của 10 dao động toàn phần.",
                "Khi chiều dài tăng, chu kỳ tăng theo quy luật nào?",
                "Chu kỳ con lắc tăng theo căn bậc hai của chiều dài và gần như không phụ thuộc khối lượng ở góc nhỏ.",
                "Hãy nhìn mốc chiều dài từ điểm treo đến tâm quả nặng.",
                "Giữ góc lệch nhỏ hơn khoảng 10° để mô hình gần đúng tốt.",
                "Đếm khi vật trở lại cùng vị trí và cùng chiều chuyển động.",
                "Dùng T = t/10, rồi so sánh với 2π√(L/g)."),
            new ExperimentLearningContent(
                "PHY_02", "NÉM XIÊN",
                "Khảo sát ảnh hưởng của góc phóng đến tầm xa, thời gian bay và độ cao cực đại.",
                "Quan sát hướng nòng phóng, vận tốc đầu và vùng rơi an toàn.",
                "Chọn góc phóng, giữ vận tốc đầu không đổi và đặt lại viên bi.",
                "Phóng bi, theo dõi quỹ đạo và ghi vị trí chạm mặt bàn.",
                "Vì sao tầm xa lớn nhất gần 45° khi điểm phóng và điểm rơi cùng độ cao?",
                "Chuyển động ngang đều kết hợp với chuyển động rơi nhanh dần đều theo phương thẳng đứng.",
                "Tách vận tốc đầu thành thành phần ngang và đứng.",
                "Thời gian bay phụ thuộc thành phần vận tốc thẳng đứng.",
                "Tầm xa bằng vận tốc ngang nhân thời gian bay.",
                "So sánh R = v₀²sin(2α)/g với số đo."),
            new ExperimentLearningContent(
                "PHY_03", "MA SÁT",
                "Phân biệt ma sát nghỉ cực đại và ma sát trượt, rồi khảo sát ảnh hưởng của lực ép.",
                "Quan sát lực kế khi vật còn đứng yên và ngay lúc bắt đầu trượt.",
                "Chọn khối lượng, đặt vật trên bề mặt và kéo lực kế theo phương ngang.",
                "Tăng lực kéo chậm, ghi lực cực đại trước khi trượt và lực khi trượt đều.",
                "Khi tăng khối lượng, hai lực ma sát thay đổi như thế nào?",
                "Ma sát nghỉ tự điều chỉnh đến một giới hạn; ma sát trượt thường nhỏ hơn và xấp xỉ μN.",
                "Kéo ngang để lực ép N gần bằng mg.",
                "Đọc đỉnh lực ngay trước lúc vật bắt đầu chuyển động.",
                "Khi vật trượt đều, lực kéo cân bằng lực ma sát trượt.",
                "Tính μ = F/N cho từng loại ma sát."),
            new ExperimentLearningContent(
                "PHY_04", "ĐO CHUYỂN ĐỘNG BẰNG CỔNG QUANG",
                "Dùng thời gian che sáng để đo vận tốc và dùng hai vị trí để ước lượng gia tốc.",
                "Quan sát bản chắn, hai cổng quang và các kênh A/B trên đồng hồ MC964.",
                "Đặt khoảng cách cổng và nhập đúng bề rộng bản chắn.",
                "Cho xe đi qua hai cổng, ghi thời gian che sáng và vận tốc ở mỗi cổng.",
                "Vận tốc tại cổng phụ thuộc vào bề rộng bản chắn và thời gian che như thế nào?",
                "Cổng quang biến một khoảng chiều dài biết trước thành phép đo vận tốc v = d/Δt.",
                "Kiểm tra đúng cổng A và B trước khi chạy.",
                "Bề rộng bản chắn phải dùng đơn vị mét.",
                "Thời gian che càng ngắn thì vận tốc càng lớn.",
                "Dùng a = (v₂²-v₁²)/(2s) nếu gia tốc gần không đổi."),
            new ExperimentLearningContent(
                "PHY_05", "DAO ĐỘNG LÒ XO",
                "Đo độ giãn tĩnh và chu kỳ để khảo sát vai trò của khối lượng và độ cứng lò xo.",
                "Quan sát vị trí tự nhiên, vị trí cân bằng mới và biên độ dao động.",
                "Chọn khối lượng, gắn chắc vào lò xo rồi đánh dấu vị trí cân bằng.",
                "Kéo nhẹ theo phương thẳng đứng, thả không đẩy và đo nhiều chu kỳ.",
                "Khi tăng khối lượng, độ giãn và chu kỳ thay đổi ra sao?",
                "Ở giới hạn đàn hồi: Δl = mg/k và T = 2π√(m/k).",
                "Không kéo quá vùng làm việc của lò xo.",
                "Đo độ giãn từ chiều dài tự nhiên đến vị trí cân bằng.",
                "Đo nhiều chu kỳ rồi chia để giảm sai số phản xạ.",
                "So sánh k tính từ mg/Δl và từ 4π²m/T²."),
            new ExperimentLearningContent(
                "PHY_06", "BẢO TOÀN ĐỘNG LƯỢNG TRÊN ĐỆM KHÍ",
                "So sánh tổng động lượng trước và sau va chạm đàn hồi hoặc dính.",
                "Quan sát chiều dương của ray, khối lượng xe và loại đầu va chạm.",
                "Chọn kiểu va chạm, đặt hai xe và kiểm tra cổng quang.",
                "Cho xe va chạm, ghi vận tốc có dấu của cả hai xe trước và sau.",
                "Tổng động lượng có được bảo toàn dù động năng có thể thay đổi không?",
                "Trong hệ gần cô lập, tổng động lượng có hướng trước và sau va chạm gần bằng nhau.",
                "Giữ dấu vận tốc theo chiều dương đã chọn.",
                "Tính từng p = mv trước khi cộng.",
                "Va chạm dính bảo toàn động lượng nhưng không bảo toàn động năng.",
                "Đánh giá sai lệch phần trăm thay vì đòi hai số tuyệt đối giống nhau.")
        };

        public static ExperimentLearningContent Get(string id) =>
            All.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}


using System;
using Sonic853.Translate;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Sonic853.Udon.Calendar
{
    public class Calendar : UdonSharpBehaviour
    {
        [SerializeField] TranslateManager translateManager;
        [SerializeField] TMP_Text yearText;
        [SerializeField] TMP_Text monthText;
        [SerializeField] Toggle[] weekToggles;
        [SerializeField] Image[] weekImages;
        [SerializeField] Image[] weekClickedImages;
        [SerializeField] TMP_Text[] weekTexts;
        [SerializeField] Toggle[] dayToggles;
        [SerializeField] Image[] dayImages;
        [SerializeField] Image[] dayClickedImages;
        [SerializeField] TMP_Text[] dayTexts;
        /// <summary>
        /// 时分秒
        /// </summary>
        [SerializeField] TMP_Text timeText;
        /// <summary>
        /// TextMeshPro 用的 默认 Material
        /// </summary>
        [SerializeField] Material defaultMaterial;
        /// <summary>
        /// TextMeshPro 用的 选中 Material，如果是当天的选不选中还是这个 Material
        /// </summary>
        [SerializeField] Material selectedMaterial;
        /// <summary>
        /// TextMeshPro 用的 不是当月的 Material
        /// </summary>
        // [SerializeField] Material notCurrentMaterial;
        /// <summary>
        /// 默认字体颜色
        /// </summary>
        [SerializeField] Color defaultColor;
        /// <summary>
        /// 选中的字体颜色，如果是当天的选不选中还是这个颜色
        /// </summary>
        [SerializeField] Color selectedColor;
        /// <summary>
        /// 不是当月的字体颜色
        /// </summary>
        [SerializeField] Color notCurrentColor;
        /// <summary>
        /// 默认图片
        /// </summary>
        [SerializeField] Sprite defaultSprite;
        /// <summary>
        /// 选中的图片
        /// </summary>
        [SerializeField] Sprite selectedSprite;
        /// <summary>
        /// 当天的图片
        /// </summary>
        [SerializeField] Sprite currentSprite;
        /// <summary>
        /// 当天选中的图片
        /// </summary>
        [SerializeField] Sprite currentSelectedSprite;
        /// <summary>
        /// 以星期几开头：
        /// 星期一为 0
        /// 星期日为 1
        /// 星期六为 2
        /// </summary>
        [SerializeField][Range(0, 2)] int weekStartDay = 0;
        [Header("0: Monday\n1: Sunday\n2: Saturday")]
        /// <summary>
        /// 使用 24 小时
        /// </summary>
        [SerializeField] bool use24Hours = true;
        [SerializeField] Toggle toggle24Hours;
        [SerializeField] GameObject aboutPanel;
        readonly string[] DaysOfWeek = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        readonly string[] Months = new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        readonly string[] AMPM = new string[] { "AM", "PM" };
        string[] daysOfWeek;
        DateTime dateTime;
        int currentYear = -1;
        int currentMonth = -1;
        int currentDay = -1;
        /// <summary>
        /// 上个月的 index
        /// </summary>
        int lastMonthIndex = -1;
        /// <summary>
        /// 下个月的 index
        /// </summary>
        int nextMonthIndex = -1;
        float runTime = 0;
        [SerializeField] float interval = 0.2f;
        void Start()
        {
            if (!translateManager.LoadedTranslate) translateManager.LoadTranslate();
            Init();
        }
        void Update()
        {
            // 每 interval 秒更新一次 dateTime
            if (runTime + interval < Time.time)
            {
                runTime = Time.time;
                CheckTime();
            }
        }
        public void Init()
        {
            dateTime = DateTime.Now;
            yearText.text = dateTime.Year.ToString();
            monthText.text = _(Months[dateTime.Month - 1]);
            toggle24Hours.SetIsOnWithoutNotify(use24Hours);
            if (use24Hours)
            {
                timeText.text = dateTime.ToString("HH:mm:ss");
            }
            else
            {
                timeText.text = $"{dateTime:hh:mm:ss} {_(AMPM[dateTime.Hour < 12 ? 0 : 1])}";
            }
            daysOfWeek = GetWeekDays(weekStartDay);
            var dayOfWeek = dateTime.DayOfWeek.ToString();
            for (var i = 0; i < weekToggles.Length; i++)
            {
                var weekToggle = weekToggles[i];
                var weekImage = weekImages[i];
                var weekClickedImage = weekClickedImages[i];
                var weekText = weekTexts[i];
                weekText.text = _(daysOfWeek[i]);
                var isThisWeekDay = dayOfWeek == daysOfWeek[i];
                weekToggle.SetIsOnWithoutNotify(isThisWeekDay);
                if (isThisWeekDay)
                {
                    weekText.fontMaterial = selectedMaterial;
                    weekText.color = selectedColor;
                    weekImage.sprite = currentSprite;
                    weekClickedImage.sprite = currentSelectedSprite;
                }
                else
                {
                    weekText.fontMaterial = defaultMaterial;
                    weekText.color = defaultColor;
                    weekImage.sprite = defaultSprite;
                    weekClickedImage.sprite = selectedSprite;
                }
            }
            // 拿到这个月的 1 号是星期几
            var firstDayOfMonth = new DateTime(dateTime.Year, dateTime.Month, 1);
            var firstDayOfWeek = firstDayOfMonth.DayOfWeek.ToString();
            var weekIndex = Array.IndexOf(daysOfWeek, firstDayOfWeek);
            // 将上个月的日期设置为 notCurrentColor
            lastMonthIndex = weekIndex - 1;
            if (lastMonthIndex >= 0)
            {
                // 上个月
                var lastMonthDay = firstDayOfMonth.AddDays(-1).Day;
                for (var i = lastMonthIndex; i >= 0; i--)
                {
                    // firstDayOfMonth 减去 lastMonthDay
                    var dayToggle = dayToggles[i];
                    var dayImage = dayImages[i];
                    var dayClickedImage = dayClickedImages[i];
                    var dayText = dayTexts[i];
                    dayText.text = lastMonthDay.ToString();
                    dayToggle.SetIsOnWithoutNotify(false);
                    dayText.color = notCurrentColor;
                    dayImage.sprite = defaultSprite;
                    dayClickedImage.sprite = selectedSprite;
                    lastMonthDay--;
                }
            }
            // 从本月的 1 号开始
            var firstDay = 1;
            currentYear = dateTime.Year;
            currentMonth = dateTime.Month;
            currentDay = dateTime.Day;
            // 本月的最后一天
            var lastDay = DateTime.DaysInMonth(currentYear, currentMonth);
            // 设置完本月后的下一格
            nextMonthIndex = -1;
            for (var i = weekIndex; i < dayToggles.Length; i++)
            {
                var dayToggle = dayToggles[i];
                var dayImage = dayImages[i];
                var dayClickedImage = dayClickedImages[i];
                var dayText = dayTexts[i];
                dayText.text = firstDay.ToString();
                var isThisDay = firstDay == currentDay;
                dayToggle.SetIsOnWithoutNotify(isThisDay);
                if (isThisDay)
                {
                    dayText.fontMaterial = selectedMaterial;
                    dayText.color = selectedColor;
                    dayImage.sprite = currentSprite;
                    dayClickedImage.sprite = currentSelectedSprite;
                }
                else
                {
                    dayText.fontMaterial = defaultMaterial;
                    dayText.color = defaultColor;
                    dayImage.sprite = defaultSprite;
                    dayClickedImage.sprite = selectedSprite;
                }
                firstDay++;
                if (firstDay > lastDay)
                {
                    nextMonthIndex = 1 + i;
                    break;
                }
            }
            // 下个月的第一天
            var nextMonthDay = 1;
            if (nextMonthIndex != -1)
            {
                for (var i = nextMonthIndex; i < dayToggles.Length; i++)
                {
                    var dayToggle = dayToggles[i];
                    var dayImage = dayImages[i];
                    var dayClickedImage = dayClickedImages[i];
                    var dayText = dayTexts[i];
                    dayText.text = nextMonthDay.ToString();
                    dayToggle.SetIsOnWithoutNotify(false);
                    dayText.color = notCurrentColor;
                    dayImage.sprite = defaultSprite;
                    dayClickedImage.sprite = selectedSprite;
                    nextMonthDay++;
                }
            }
        }
        public void CheckTime()
        {
            dateTime = DateTime.Now;
            if (use24Hours)
            {
                timeText.text = dateTime.ToString("HH:mm:ss");
            }
            else
            {
                // timeText.text = $"{dateTime:hh:mm:ss} {_(AMPM[dateTime.Hour < 12 ? 0 : 1])}";
                timeText.text = string.Format(_("{0} {1}"), dateTime.ToString("hh:mm:ss"), _(AMPM[dateTime.Hour < 12 ? 0 : 1]));
            }
            if (currentDay != dateTime.Day || currentMonth != dateTime.Month || currentYear != dateTime.Year)
            {
                Init();
            }
        }
        public void UIWeekClick()
        {
            dateTime = DateTime.Now;
            var dayOfWeek = dateTime.DayOfWeek.ToString();
            for (var i = 0; i < weekToggles.Length; i++)
            {
                var weekText = weekTexts[i];
                var weekToggle = weekToggles[i];
                var selected = weekToggle.isOn;
                var isThisWeekDay = dayOfWeek == daysOfWeek[i];
                weekText.fontMaterial = selected || isThisWeekDay ? selectedMaterial : defaultMaterial;
                weekText.color = selected || isThisWeekDay ? selectedColor : defaultColor;
            }
        }
        public void UIDayClick()
        {
            dateTime = DateTime.Now;
            for (var i = 0; i < dayToggles.Length; i++)
            {
                var dayText = dayTexts[i];
                var dayToggle = dayToggles[i];
                var selected = dayToggle.isOn;
                var isThisDay = dayText.text == currentDay.ToString() && lastMonthIndex < i && i < nextMonthIndex;
                dayText.fontMaterial = selected || isThisDay ? selectedMaterial : defaultMaterial;
                dayText.color = selected || isThisDay ? selectedColor : i <= lastMonthIndex || i >= nextMonthIndex ? notCurrentColor : defaultColor;
                if (selected)
                {
                    var weekToggle = weekToggles[i % 7];
                    weekToggle.isOn = true;
                }
            }
        }
        public void UI24HoursClick()
        {
            use24Hours = toggle24Hours.isOn;
            CheckTime();
        }
        string[] GetWeekDays(int weekStartDay)
        {
            var _weekStartDay = 7;
            // 确保输入合法
            if (weekStartDay < 0 || weekStartDay > 2)
            {
                weekStartDay = 0;
            }
            _weekStartDay -= weekStartDay;
            if (_weekStartDay == 7) _weekStartDay = 0;

            // 创建新的数组来存放结果
            string[] reorderedDays = new string[7];

            // 根据 weekStartDay 填充数组
            for (var i = 0; i < 7; i++)
            {
                reorderedDays.SetValue(DaysOfWeek[(_weekStartDay + i) % 7], i);
            }

            return reorderedDays;
        }
        public void ToggleAboutPanel() => aboutPanel.SetActive(!aboutPanel.activeSelf);
        string _(string text) => translateManager.GetText(text);
    }
}

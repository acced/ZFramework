using System;
using System.Text.RegularExpressions;

namespace ZFrameWork.Utils
{
    public static class StringUtil
    {
        
        #region 判断string
        /// <summary>
        /// 判断对象是否为Null、DBNull、Empty或空白字符串
        /// </summary>
        /// <param name="value">判断对象</param>
        /// <returns>判断返回值</returns>
        public static bool IsNullOrEmpty(string value)
        {
            bool retVal = value == null || string.IsNullOrWhiteSpace(value);
            return retVal;
        }
        /// <summary>
        /// 检查是否符合Email格式
        /// </summary>
        /// <param name="email"></param>
        /// <returns>判断返回值</returns>
        public static bool IsEmail(this string email)
        {
            Regex regex = new Regex("[a-zA-Z_0-9]+@[a-zA-Z_0-9]{2,6}(\\.[a-zA-Z_0-9]{2,3})+");
            return regex.IsMatch(email);
        }
        /// <summary>
        /// 检查是否符合PhoneNumber格式
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns>判断返回值</returns>
        public static bool IsPhoneNumber(this string strInput)
        {
            Regex reg = new Regex(@"(^\d{11}$)");
            return reg.IsMatch(strInput);
        }
        
        /// <summary>
        /// 检查后缀名         e.g. // 检查 "HelloWorld.txt" 是否以 ".txt" 结尾   result = "HelloWorld.txt".IsSuffix(".txt");  // result = true
        /// </summary>
        /// <param name="str"></param>
        /// <param name="suffix"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static bool IsSuffix(this string str, string suffix, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            //总长度减去后缀的索引等于后缀的长度
            int indexOf = str.LastIndexOf(suffix, StringComparison.CurrentCultureIgnoreCase);
            return indexOf != -1 && indexOf == str.Length - suffix.Length;
        }
        
        #endregion
    
    }
}
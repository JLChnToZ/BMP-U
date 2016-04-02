using System;
using System.Collections.Generic;

namespace BMS {
    public static class Base36 {
        private const string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";

        public static string Encode(int input) {
            if(input < 0) throw new ArgumentOutOfRangeException("input", input, "input cannot be negative");
            var result = new Stack<char>();
            do {
                result.Push(CharList[input % 36]);
                input /= 36;
            } while(input > 0);
            return new string(result.ToArray());
        }

        public static int Decode(string input) {
            int result = 0;
            int pos = input.Length - 1;
            int idx;
            foreach(char c in input.ToLower()) {
                idx = CharList.IndexOf(c);
                if(idx < 0) return -1;
                result += idx * (int)Math.Pow(36, pos);
                pos--;
            }
            return result;
        }
    }
}

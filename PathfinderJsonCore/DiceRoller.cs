using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Security.Cryptography;

namespace PathfinderJson
{
    public static class DiceRoller
    {
        
        public static (string, double) RollDice(string diceString)
        {
            var rng = new RNGCryptoServiceProvider();

            diceString = diceString.Replace(" ", "").Replace("\n", "");
            

            string number = "";

            string count = "";
            string dsize = "";

            string resultValue = "";

            bool dsizeMode = false;

            // first, go through each character in the string, picking out any dice syntax
            foreach (char c in diceString)
            {
                if (char.IsDigit(c) || c == '-')
                {
                    number += c;
                }
                else
                {
                    string num = number;
                    number = "";

                    if (dsizeMode)
                    {
                        if (string.IsNullOrWhiteSpace(num))
                        {
                            // nothing to use?
                        }
                        else
                        {
                            dsize = num;
                            (int res, int[] arr) = DoAllDiceRolls(int.Parse(count), int.Parse(dsize), rng);
                            resultValue += "(" + string.Join("+", arr) + ")";
                        }
                        dsizeMode = false;

                        resultValue += c;
                    }
                    else
                    {
                        if (char.ToLowerInvariant(c) == 'd')
                        {
                            if (string.IsNullOrWhiteSpace(num))
                            {
                                // nothing to use?
                            }
                            else
                            {
                                count = num;
                                dsizeMode = true;
                            }
                        }
                        else
                        {
                            resultValue += c;
                        }
                    }
                }
            }

            string finalNum = number;
            number = "";

            if (dsizeMode)
            {
                if (string.IsNullOrWhiteSpace(finalNum))
                {
                    // nothing to use?
                }
                else
                {
                    dsize = finalNum;
                    (int res, int[] arr) = DoAllDiceRolls(int.Parse(count), int.Parse(dsize), rng);
                    resultValue += "(" + string.Join("+", arr) + ")";
                }
                dsizeMode = false;
            }
            else
            {
                resultValue += finalNum;
            }

            // then, just run the remaining math
            return (resultValue, ArithmeticParser.Evaluate(resultValue));
        }

        private static (int, int[]) DoAllDiceRolls(int count, int size, RNGCryptoServiceProvider rng)
        {
            int result = 0;
            int[] arr = new int[count];
    
            for (int i = 0; i < count; i++)
            {
                int v = DoOneDiceRoll(rng, size) + 1;
                result += v;
                arr[i] = v;
            }

            return (result, arr);
        }

        private static int DoOneDiceRoll(RNGCryptoServiceProvider rnd, int max)
        {
            byte[] r = new byte[4];
            int value;
            do
            {
                rnd.GetBytes(r);
                value = BitConverter.ToInt32(r, 0) & int.MaxValue;
            } while (value >= max * (int.MaxValue / max));
            return value % max;
        }

    }
}

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
            var rng = RandomNumberGenerator.Create();// RNGCryptoServiceProvider();

            diceString = diceString.Replace(" ", "").Replace("\n", "");
            
            // quick exit for situations where there aren't any dice
            if (ArithmeticParser.IsValidString(diceString))
            {
                return (diceString, ArithmeticParser.Evaluate(diceString));
            }

            string number = "";

            string count = "";
            string dsize = "";

            string resultValue = "";

            bool dsizeMode = false;
            bool prevNumber = false;

            // first, go through each character in the string, picking out any dice syntax
            foreach (char c in diceString)
            {
                if (char.IsDigit(c))
                {
                    // this is a number
                    // store it and move in
                    number += c;
                    prevNumber = true;
                }
                else if (c == '-')
                {
                    // special handling for the hyphen
                    // as it could be symbolizing a negative number or a subtraction operation
                    if (prevNumber)
                    {
                        // this is a subtraction operation
                        resultValue += number;
                        resultValue += c;
                        number = "";
                        prevNumber = false;
                    }
                    else
                    {
                        // this is a negative number
                        number += c;
                        prevNumber = false;
                    }
                }
                else
                {
                    // not a numeric character
                    // whatever number we have stored, keep it off to the side and clear out the buffer
                    string num = number;
                    number = "";
                    prevNumber = false;

                    if (dsizeMode)
                    {
                        // is currently in dice-size mode
                        // the "6" in 2d6
                        // at this point, we've reached a non-numeric character, which means we should now act upon the number we've currently stored
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
                        // not in dice-size mode
                        if (char.ToLowerInvariant(c) == 'd')
                        {
                            // currently defining a die
                            // the stored number is the number of dice to roll (the "2" in 2d6)
                            // so now, we store this as the number of dice to roll, enter dice-size mode, and move on
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
                            // not defining a die
                            // probably just an operator or a parantheses
                            // store the currently-stored number and move on
                            resultValue += num;

                            // also, store the current character
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

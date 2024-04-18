using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ZFrameWork.Scripts
{
    public class TestAsy : MonoBehaviour
    {
        internal class Bacon { }
        internal class Coffee { }
        internal class Egg { }
        internal class Juice { }
        internal class Toast { }
        
        private async void Start()
        {
            Coffee cup = PourCoffee();
            Debug.Log("coffee is ready");

            var eggsTask = FryEggsAsync(2);
            var baconTask = FryBaconAsync(3);
            var toastTask = MakeToastWithButterAndJamAsync(2);

            var eggs = await eggsTask;
            Debug.Log("eggs are ready");

            var bacon = await baconTask;
            Debug.Log("bacon is ready");

            var toast = await toastTask;
            Debug.Log("toast is ready");

            Juice oj = PourOJ();
            Debug.Log("oj is ready");
            Debug.Log("Breakfast is ready!");

        }
        
        static async Task<Toast> MakeToastWithButterAndJamAsync(int number)
        {
            var toast = await ToastBreadAsync(number);
            ApplyButter(toast);
            ApplyJam(toast);

            return toast;
        }

        private static Juice PourOJ()
        {
            Debug.Log("Pouring orange juice");
            return new Juice();
        }

        private static void ApplyJam(Toast toast) =>
            Debug.Log("Putting jam on the toast");

        private static void ApplyButter(Toast toast) =>
            Debug.Log("Putting butter on the toast");

        private static async Task<Toast> ToastBreadAsync(int slices)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                Debug.Log("Putting a slice of bread in the toaster");
            }
            Debug.Log("Start toasting...");
            await Task.Delay(3000);
            Debug.Log("Remove toast from toaster");

            return new Toast();
        }

        private static async Task<Bacon> FryBaconAsync(int slices)
        {
            Debug.Log($"putting {slices} slices of bacon in the pan");
            Debug.Log("cooking first side of bacon...");
            await Task.Delay(3000);
            for (int slice = 0; slice < slices; slice++)
            {
                Debug.Log("flipping a slice of bacon");
            }
            Debug.Log("cooking the second side of bacon...");
            await Task.Delay(3000);
            Debug.Log("Put bacon on plate");

            return new Bacon();
        }

        private static async Task<Egg> FryEggsAsync(int howMany)
        {
            Debug.Log("Warming the egg pan...");
            await Task.Delay(3000);
            Debug.Log($"cracking {howMany} eggs");
            Debug.Log("cooking the eggs ...");
            await Task.Delay(3000);
            Debug.Log("Put eggs on plate");

            return new Egg();
        }

        private static Coffee PourCoffee()
        {
            Debug.Log("Pouring coffee");
            return new Coffee();
        }
    }
}
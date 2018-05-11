﻿using CarParking.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CarParking.Classes
{
    class Parking : IPark
    {
        private static readonly Lazy<Parking> instance = new Lazy<Parking>(() => new Parking());
        private Parking()
        {
            Settings.SetSettings(this);
            Cars = new List<ICar>(this.ParkingSpace);
            Transactions = new List<ITransaction>();
            SetTimerForLog(60000);
            SetTimerForPayments(TimeOut);
        }
        public static Parking Instance { get { return instance.Value; } }


        #region IPark Members

        public int TimeOut { get; set; }

        public Dictionary<CarType, double> Prices { get; set; }

        public float Fine { get; set; }

        public int ParkingSpace { get; set; }

        public List<ICar> Cars { get; set; }

        public List<ITransaction> Transactions { get; set; }
        private void SetTimerForLog(int interval)
        {
            int timeout = 0;
            TimerCallback callback = new TimerCallback(WriteToLog);
            Timer timer = new Timer(callback, null, timeout, interval);
        }
        private void SetTimerForPayments(int interval)
        {
            int timeout = 0;
            TimerCallback callback = new TimerCallback(WriteOffRent);
            Timer timer = new Timer(callback, null, timeout, interval);
        }
        private void WriteToLog(object state)
        {
            DateTime currentTime = DateTime.Now;
            DateTime timePoint = currentTime.Subtract(new TimeSpan(0, 1, 0));
            string message = String.Empty;
            if (Transactions != null && Transactions.Count > 0)
            {
                var history = Transactions.FindAll(t => (t.Time >= timePoint));
                if (history != null)
                {
                    double sum = 0;
                    foreach (ITransaction item in history)
                    {
                        sum += item.WrittenOffAmount;
                    }
                    message += String.Format("\t{0:d} at {0:t}\tAmount:\t{1:C3}\n", currentTime, sum);
                }

            }
            else
            {
                return;
            }

            string folderpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filepath = Path.Combine(folderpath, "Transactions.log");

            if (File.Exists(filepath))
            {
                using (StreamWriter outputFile = new StreamWriter(filepath, true))
                {
                    outputFile.WriteLine(message);
                }
            }
            else
            {
                using (StreamWriter outputFile = new StreamWriter(filepath))
                {
                    outputFile.WriteLine(message);
                }
            }


        }
        private void WriteOffRent(object state)
        {
            if (Cars != null)
            {
                for (int index = 0; index < Cars.Count; index++)
                {
                    ICar currentCar = Cars[index];
                    if (currentCar != null)
                    {
                        double payment = 0.0;
                        payment = Prices[currentCar.CarType];
                        if (currentCar.CarBalance < payment)
                        {
                            payment = payment * (1 + Fine);
                        }
                        currentCar.PayRent(payment);
                        Transactions.Add(new Transaction(DateTime.Now, currentCar.CarId, payment));
                    }

                }
            }
        }

        public bool AddCar(ICar car)
        {
            if (car != null && Cars.Count < Cars.Capacity)
            {
                Cars.Add(car);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool RemoveCar(int id)
        {
            ICar thatCar = Cars.Find(c => c.CarId == id);
            if (thatCar != null && thatCar.CarBalance >= 0)
            {
                Cars.Remove(thatCar);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddToBalance(int id, double amount)
        {
            ICar thatCar = Cars.Find(c => c.CarId == id);
            if (thatCar != null)
            {
                thatCar.AddToBalance(amount);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string ShowTransactionsFor(int minute = 1)
        {
            DateTime currentTime = DateTime.Now;
            DateTime timePoint = currentTime.Subtract(new TimeSpan(0, minute, 0));
            var history = Transactions.FindAll(t => (t.Time >= timePoint));
            string result = String.Empty;
            if (history != null)
            {
                result += String.Format("\t\tHere is the history of transaction for last {0} minute/s:\n \t{1,-15}\t{2,-12}\t{3}\n", minute, "Date &Time", "Car id", "Amount");
                foreach (ITransaction t in history)
                {
                    result += String.Format("\t{0:d} at {0:T}\t\t{1}\t\t{2:C3}\n", t.Time, t.CarId.ToString(), t.WrittenOffAmount.ToString());
                }
            }
            else
            {
                result = "There is no history of transactions for such period.";
            }
            return result;
        }

        public string ShowIncome()
        {
            string result = String.Empty;
            double income = 0;
            foreach (ITransaction item in Transactions)
            {
                income += item.WrittenOffAmount;
            }

            result = "Income: " + income.ToString("");
            return result;
        }

        public int ShowFreePlaces()
        {
            return ParkingSpace - Cars.Count;
        }

        public string[] ShowLog()
        {
            string[] readLog = new string[] { String.Empty };
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = Path.Combine(folderPath, "Transactions.log");

            if (File.Exists(filePath))
            {
                readLog = File.ReadAllLines(filePath);
                return readLog;
            }
            else
            {
                return null;
            }
        }

        public bool HasCar(int id)
        {
            return (Cars.Find(c => c.CarId == id) != null) ? true : false;
        }

        #endregion

    }
}

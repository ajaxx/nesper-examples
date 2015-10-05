using System;
using System.Threading;

using com.espertech.esper.client;

namespace SlidingWindow
{
    class Program
    {
        private static Timer _eventTimer;
        private static Random _random;

        private static EPAdministrator _administrator;
        private static EPRuntime _runtime;

        static void Main(string[] args)
        {
            InitializeEsper();
            InitializeEvents();

            // Make the application hang around - failure to do this will result
            // in the application terminating because there are no non-daemon
            // threads running.
            Console.WriteLine("Application waiting until you hit return to end");
            Console.ReadLine();
        }

        /// <summary>
        /// Initializes Esper and prepares event listeners.
        /// </summary>
        static void InitializeEsper()
        {
            var serviceProvider = EPServiceProviderManager.GetDefaultProvider();
            _runtime = serviceProvider.EPRuntime;
            _administrator = serviceProvider.EPAdministrator;
            // You must tell esper about your events, failure to do so means you will be using
            // the fully qualified names of your events.  There are many ways to do that, but
            // this one is short-hand which makes the name of your event aliased to the fully
            // qualified name of your type.
            _administrator.Configuration.AddEventType<TradeEvent>();
            // Create a sliding window.  Sliding windows will keep around events for a set duration
            // of time.  Events will enter (insert) and leave (remove) and you can determine
            // how you will see those.
            var statement = _administrator.CreateEPL("select irstream *, max(Price) from TradeEvent.win:time(5 sec)");
            // Hook up an event handler to the statement
            statement.Events += new UpdateEventHandler(HandleEvent);
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="com.espertech.esper.client.UpdateEventArgs"/> instance containing the event data.</param>
        private static void HandleEvent(object sender, UpdateEventArgs args)
        {
            // if you have a window or a complex rule, you will find that the event args contains
            // information about events that are no longer applicable and those that are currently
            // applicable.  For now, we only care about those events that are directly applicable.

            if (args.OldEvents != null)
            {
                foreach (var e in args.OldEvents)
                {
                    Console.WriteLine("<< TradeEvent - Symbol: {0}, Quantity: {1}, Price: {2}, Max: {3}",
                        e.Get("Symbol"),
                        e.Get("Quantity"),
                        e.Get("Price"),
                        e.Get("max(Price)"));
                }
            }

            if (args.NewEvents != null)
            {
                foreach (var e in args.NewEvents)
                {
                    Console.WriteLine(">> TradeEvent - Symbol: {0}, Quantity: {1}, Price: {2}, Max: {3}",
                        e.Get("Symbol"),
                        e.Get("Quantity"),
                        e.Get("Price"),
                        e.Get("max(Price)"));
                }
            }
        }

        /// <summary>
        /// Esper requires events to come from somewhere; this application is just a dummy
        /// application so we have to manufacture the events.  In a real application, you
        /// would receive these from "something" that produces these events.
        /// </summary>
        static void InitializeEvents()
        {
            _random = new Random();
            _eventTimer = new Timer(
                o => SendTradeEvent(),
                null,
                250,
                250);

        }

        private static int SequenceNo = 0;

        /// <summary>
        /// Sends the trade event.
        /// </summary>
        static void SendTradeEvent()
        {
            var radians = (SequenceNo*Math.PI)/180.0;
            var sin = Math.Sin(radians);

            SequenceNo++;

            // Create a phantom event
            var tradeEvent = new TradeEvent()
            {
                Symbol = "GOOG",
                Price = 700 + Math.Round(100 * sin),
                Quantity = 100 + (int) Math.Round(100 * sin)
            };

            // Events must be injected into the runtime
            _runtime.SendEvent(tradeEvent);
        }
    }

    public class TradeEvent
    {
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("Symbol: {0}, Quantity: {1}, Price: {2}", Symbol, Quantity, Price);
        }
    }
}

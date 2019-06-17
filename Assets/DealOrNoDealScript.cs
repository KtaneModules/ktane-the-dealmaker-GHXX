using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DealOrNoDealScript : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombInfo BombInfo;

    public KMSelectable ButtonDeal;
    public KMSelectable ButtonRenew;
    public TextMesh DealDisplayText;


    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private char nbsp = ' ';

    private const int displayTextLineLength = 19;
    private const int goodDealsToPass = 4;

    private static System.Random rand = new System.Random();

    private static readonly List<DealItem> PriceList = new List<DealItem>()
    {
        new DealItem("steel","steel",1,false),
        new DealItem("copper","copper",2,false),
        new DealItem("alpaca","fluffy alpaca",25,true),
        new DealItem("fakegoldingot","fake gold ingot with copper core",12,true),
        new DealItem("coin","coin",5,true),
    };


    private bool isGoodDeal;
    private int goodDealsMade = 0;

    public void Awake()
    {
        this.moduleId = moduleIdCounter++;

        RenewDeal();

        this.ButtonDeal.OnInteract += () => { ButtonDealPress(); return false; };
        this.ButtonRenew.OnInteract += () => { ButtonRenewPress(); return false; };
    }

    private void ButtonRenewPress()
    {
        if (this.moduleSolved)
            return;

        RenewDeal();
    }

    private void ButtonDealPress()
    {
        if (this.moduleSolved)
            return;

        Log("Deal Pressed. Checking result.");
        if (!this.isGoodDeal)
        {
            GetComponent<KMBombModule>().HandleStrike();
            RenewDeal();
        }
        else
        {
            if (++this.goodDealsMade >= goodDealsToPass)
            {
                this.moduleSolved = true;
                DealDisplayText.text = ""; // clear display

                GetComponent<KMBombModule>().HandlePass();
            }
            else
            {
                RenewDeal();
            }
        }
    }

    // Use this for initialization
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Log(string message)
    {
        Debug.Log("[DealOrNoDeal #" + this.moduleId + "] " + message);
    }

    private void RenewDeal() // TODO discount if solved module number is uneven? with led
    {
        bool makeGoodDeal = rand.Next(0, 2) == 1;

        Log("Attempting to generate " + (makeGoodDeal ? "good" : "bad") + " deal.");

        var purchaseItem = PriceList.PickRandom();

        double upperLimit = 1.2;
        double lowerLimit = 0.8;

        double factor = rand.NextDouble() * (upperLimit - lowerLimit) + lowerLimit;

        double newUnitPrice = purchaseItem.value * factor;

        double qty = 0;

        if (purchaseItem.countable)
            qty = rand.Next(1, 16);
        else
            qty = Math.Round(rand.NextDouble() * 99.5 + 0.5, 2);

        double totalPrice = Math.Round(newUnitPrice * qty, 2);

        var actualUnitFactor = totalPrice / (purchaseItem.value * qty);
        bool buyIsGood = actualUnitFactor < 1;
        bool isThisASellDeal = buyIsGood ^ makeGoodDeal;

        this.isGoodDeal = makeGoodDeal;

        string displayText = (isThisASellDeal ? "Sell " : "Buy ") + qty + " " + purchaseItem.friendlyName + " for " + totalPrice + this.nbsp + "CUR.";


        string wrappedDisplayText = "";
        string remainingText = displayText;
        while (remainingText.Length != 0)
        {
            if (remainingText.Length <= displayTextLineLength)
            {
                wrappedDisplayText += remainingText;
                break;
            }

            int[] wrapIndexes = Enumerable.Range(0, remainingText.Count()).Where(x => remainingText[x] == ' ').ToArray();
            int lastGoodIndex = 0;

            try
            {
                lastGoodIndex = wrapIndexes.Last(x => x <= displayTextLineLength);
            }
            catch (InvalidOperationException) // if no good index was found
            {
                if (wrapIndexes.Any())
                {
                    lastGoodIndex = wrapIndexes.First();
                }
                else
                {
                    wrappedDisplayText += remainingText;
                    break;
                }
            }
            wrappedDisplayText += remainingText.Substring(0, lastGoodIndex) + "\n";
            remainingText = remainingText.Remove(0, lastGoodIndex + 1);
        }

        this.DealDisplayText.text = wrappedDisplayText;
        Log("Deal is " + (this.isGoodDeal ? "good" : "bad"));
    }
}

public class DealItem
{
    internal readonly string name;
    internal readonly string friendlyName;
    internal readonly int value;
    internal readonly bool countable;

    private DealItem() { }

    public DealItem(string name, string friendlyName, int value, bool countable)
    {
        this.name = name;
        this.friendlyName = friendlyName;
        this.value = value;
        this.countable = countable;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TheDealmakerScript : MonoBehaviour
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
    private const int goodDealsToPass = 2;
    private const int badDealsToAllowSingleGoodDealPass = 1;

    private static System.Random rand = new System.Random();

    private static readonly List<DealItem> PriceList = new List<DealItem>()
    {
        new DealItem("shilling", "shillings", 0.06f, true),
        new DealItem("wood", "wood", 0.5f, false),
        new DealItem("iron", "iron", 0.7f, false),
        new DealItem("steel", "steel", 0.8f, false),
        new DealItem("can of worms", "cans of worms", 1.8f, true),
        new DealItem("copper", "copper", 3.2f, false),
        new DealItem("coin", "coins", 9.4f, true),
        new DealItem("cat", "cats", 10.4f, true),
        new DealItem("fake gold ingot with copper core", "fake gold ingots with copper core", 12.8f, true),
        new DealItem("fluffy alpaca", "fluffy alpacas", 20.5f, true),
        new DealItem("abort-button", "abort-buttons", 26f, true),
        new DealItem("empty bomb case", "empty bomb cases", 35f, true),
        new DealItem("old phone", "old phones", 48f, true),
        new DealItem("hypercube", "hypercubes", 64.7f, true),
    };

    private static readonly List<Unit> UnitList = new List<Unit>()
    {
        // weight
        new Unit("gram of", "grams of", 0.001f, false),
        new Unit("esterling of", "esterling of", 0.001415f, false),
        new Unit("pennweight of", "pennweights of", 0.00155517384f, false),
        new Unit("kilogram of", "kilograms of", 1, false),
        new Unit("stone weight of", "stone weights of", 6.35029318f, false),
        new Unit("Babylonian talent of", "Babylonian talents of", 30.2f, false),
        new Unit("zentner of", "zentner of", 50, false),


        // amount
        new Unit("", "", 1, true),
        new Unit("full hand of", "full hands of", 5, true),
        new Unit("dozen", "dozen", 12, true),
        new Unit("score of", "scores of", 20, true),
        new Unit("great hundred", "great hundred", 120, true),
        new Unit("small gross", "small gross", 120, true),
        new Unit("gross", "gross", 144, true),
        new Unit("great gross", "great gross", 1728, true),
    };

    private static readonly List<Currency> CurrencyList = new List<Currency>()
    {
        new Currency("SEK", 0.09f),
        new Currency("NOK", 0.10f),
        new Currency("DKK", 0.13f),
        new Currency("PLN", 0.23f),
        new Currency("PEN", 0.27f),
        new Currency("WST", 0.34f),
        new Currency("BYN", 0.43f),
        new Currency("AUD", 0.61f),
        new Currency("CAD", 0.67f),
        new Currency("CHF", 0.89f),
        new Currency("USD", 0.89f),
        new Currency("EUR", 1),
        new Currency("IMP", 1.11f),
        new Currency("GBP", 1.12f),
    };

    private bool isGoodDeal;
    private int badDealsEncountered = 0; // enounter a bad deal and then make a good deal to win, or make 2 good deals
    private int goodDealsMade = 0;
    private string displayText = "";

    private void ModuleActivated()
    {
        this.moduleId = moduleIdCounter++;

        RenewDeal();

        this.ButtonDeal.OnInteract += () => { ButtonDealPress(); return false; };
        this.ButtonRenew.OnInteract += () => { ButtonRenewPress(); return false; };

    }

    IEnumerator DoButtonPressAndRelease(KMSelectable button)
    {
        button.AddInteractionPunch(1);
        button.transform.Translate(0, 0, -0.01f);
        this.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        yield return new WaitForSeconds(1);
        button.transform.Translate(0, 0, 0.01f);
        this.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, button.transform);
    }

    private void ButtonRenewPress()
    {
        if (this.moduleSolved || this.textIsStillUpdating)
            return;

        StartCoroutine(DoButtonPressAndRelease(this.ButtonRenew));

        RenewDeal();
    }

    private void ButtonDealPress()
    {
        if (this.moduleSolved || this.textIsStillUpdating)
            return;

        StartCoroutine(DoButtonPressAndRelease(this.ButtonDeal));

        Log("Deal Pressed. Checking result.");
        if (!this.isGoodDeal)
        {
            GetComponent<KMBombModule>().HandleStrike();
            RenewDeal();
        }
        else
        {
            if (++this.goodDealsMade >= goodDealsToPass || this.badDealsEncountered > badDealsToAllowSingleGoodDealPass) // pass if enough good deals were made, or if one bad and one good was made.
            {
                this.moduleSolved = true;
                ClearDisplay(true); // clear display fast

                GetComponent<KMBombModule>().HandlePass();
            }
            else
            {
                ClearDisplay(false); // clear display slowly
                RenewDeal();
            }
        }
    }

    private void ClearDisplay(bool fast)
    {
        if (fast)
            this.DealDisplayText.text = "";

        this.displayText = "";
    }

    // Use this for initialization
    public void Start()
    {
        this.DealDisplayText.text = ""; // fastclear
        GetComponent<KMBombModule>().OnActivate += ModuleActivated;
    }

    float i = 0;
    const int framesPerUpdate = 10;
    bool textIsStillUpdating = false;
    public void Update()
    {
        this.i += Time.deltaTime * 60;
        if (this.i > framesPerUpdate)
        {
            this.i %= framesPerUpdate;
            if (this.DealDisplayText.text.Length == 0 || this.displayText.StartsWith(this.DealDisplayText.text)) // add a letter
            {
                if (this.DealDisplayText.text.Length < this.displayText.Length) // if shown text is shorter than the desired text.
                    this.DealDisplayText.text += this.displayText[this.DealDisplayText.text.Length];
            }
            else if (this.DealDisplayText.text.Length > 0) // remove a letter
            {
                this.DealDisplayText.text = this.DealDisplayText.text.Remove(this.DealDisplayText.text.Length - 1);
                this.i = framesPerUpdate / 2; // speed up clearing
            }

            this.textIsStillUpdating = this.DealDisplayText.text != this.displayText;
        }
    }


    private void Log(string message)
    {
        Debug.Log("[DealOrNoDeal #" + this.moduleId + "] " + message);
    }

    int consecutiveBadDealAmount = 2;
    private void RenewDeal() // TODO discount if solved module number is uneven? with led
    {
        float lastMinuteOdds = this.BombInfo.GetTime() < 60 ? 0.25f : 0;

        if (lastMinuteOdds > 0)
            Log("Last minute deal! Bonus odds: " + (lastMinuteOdds * 100).ToString("N2") + "%");

        var chanceForBadDeal = Math.Pow(0.5, this.consecutiveBadDealAmount * 0.5) - lastMinuteOdds;
        bool makeGoodDeal = rand.NextDouble() > chanceForBadDeal; // 50% chance for a good deal at first. Incrases the more bad deals were encountered. Decreases on good deal.


        if (!makeGoodDeal)
            this.consecutiveBadDealAmount++;
        else
            this.consecutiveBadDealAmount--;

        Log("Attempting to generate " + (makeGoodDeal ? "good" : "bad") + " deal. Chance for a bad deal was " + (chanceForBadDeal * 100).ToString("N2") + "%.");

        var purchaseItem = PriceList.PickRandom();
        var currency = CurrencyList.PickRandom();

        var unit = UnitList.Where(x => x.countable == purchaseItem.countable).PickRandom();

        double upperLimit = 1.2;
        double lowerLimit = 0.8;

        double factor = rand.NextDouble() * (upperLimit - lowerLimit) + lowerLimit;

        double newUnitPrice = purchaseItem.value / currency.currencyValue;

        double qty = 0;

        if (purchaseItem.countable)
            qty = rand.Next(1, 16);
        else
            qty = Math.Round(rand.NextDouble() * 99.5 + 0.5, 2);

        double totalPrice = Math.Round(newUnitPrice * qty * unit.unitValue * factor, 2);


        // verify
        var actualUnitFactor = totalPrice * currency.currencyValue / (purchaseItem.value * qty * unit.unitValue);
        Log("actual Unit factor: " + actualUnitFactor);
        bool buyIsGood = actualUnitFactor < 1;
        bool isThisASellDeal = buyIsGood ^ makeGoodDeal;

        this.isGoodDeal = makeGoodDeal;

        string displayText = (isThisASellDeal ? "Sell " : "Buy ") + qty + " " + (qty != 1 ? unit.unitNamePlural : unit.unitName) + " " + (qty == 1 && unit.unitValue == 1 ? purchaseItem.friendlyName : purchaseItem.pluralFriendlyName) + " for " + totalPrice + this.nbsp + currency.currencyName + ".";


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

        this.displayText = wrappedDisplayText; // slow write
        Log("Deal is " + (this.isGoodDeal ? "good" : "bad"));
        if (!this.isGoodDeal)
            this.badDealsEncountered++;
    }
}

internal class Currency
{
    internal readonly string currencyName;
    internal readonly float currencyValue;

    private Currency() { }

    public Currency(string currencyName, float currencyValue)
    {
        this.currencyName = currencyName;
        this.currencyValue = currencyValue;
    }
}

internal class Unit
{
    internal readonly string unitName;
    internal readonly string unitNamePlural;
    internal readonly float unitValue;
    internal readonly bool countable;

    private Unit() { }

    public Unit(string unitName, string unitNamePlural, float unitvalue, bool countable)
    {
        this.unitName = unitName;
        this.unitNamePlural = unitNamePlural;
        this.unitValue = unitvalue;
        this.countable = countable;
    }
}

public class DealItem
{
    internal readonly string friendlyName;
    internal readonly string pluralFriendlyName;
    internal readonly float value;
    internal readonly bool countable;

    private DealItem() { }

    public DealItem(string friendlyName, string pluralFriendlyName, float value, bool countable)
    {
        this.friendlyName = friendlyName;
        this.pluralFriendlyName = pluralFriendlyName;
        this.value = value;
        this.countable = countable;
    }
}
using Algorand;
using Algorand.Algod.Model;
using Algorand.Algod.Model.Transactions;

namespace SimpleAlgorandStream.Model
{


    // TealValue contains type information and a value, representing a value in a
    // TEAL program
    public partial class TealValue
    {

        [Newtonsoft.Json.JsonProperty("tt", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ulong Type { get; set; }

        [Newtonsoft.Json.JsonProperty("tb", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Bytes { get; set; }

        [Newtonsoft.Json.JsonProperty("ui", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ulong Uint { get; set; }
    }

    // TealKeyValue represents a key/value store for use in an application's
    // LocalState or GlobalState
    //
    //msgp:allocbound TealKeyValue EncodedMaxKeyValueEntries
    public partial class TealKeyValue : Dictionary<string, TealValue>
    {
    }

    // StateSchemas is a thin wrapper around the LocalStateSchema and the
    // GlobalStateSchema, since they are often needed together
    public partial class StateSchemas
    {
        [Newtonsoft.Json.JsonProperty("lsch", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public StateSchema LocalStateSchema { get; set; }

        [Newtonsoft.Json.JsonProperty("gsch", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public StateSchema GlobalStateSchema { get; set; }
    }

    // AppParams stores the global information associated with an application
    public partial class AppParams
    {

        [Newtonsoft.Json.JsonProperty("approv", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public byte[] ApprovalProgram { get; set; }

        [Newtonsoft.Json.JsonProperty("clearp", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public byte[] ClearStateProgram { get; set; }

        [Newtonsoft.Json.JsonProperty("gs", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TealKeyValue GlobalState { get; set; }
        public StateSchemas StateSchemas { get; set; }

        [Newtonsoft.Json.JsonProperty("epp", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public uint ExtraProgramPages { get; set; }
    }

    // AppLocalState stores the LocalState associated with an application. It also
    // stores a cached copy of the application's LocalStateSchema so that
    // MinBalance requirements may be computed 1. without looking up the
    // AppParams and 2. even if the application has been deleted
    public partial class AppLocalState
    {

        [Newtonsoft.Json.JsonProperty("hsch", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public StateSchema Schema { get; set; }

        [Newtonsoft.Json.JsonProperty("tkv", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TealKeyValue KeyValue { get; set; }
    }

    // AppLocalStateDelta tracks a changed AppLocalState, and whether it was deleted
    public partial class AppLocalStateDelta
    {
        public AppLocalState LocalState { get; set; }
        public bool Deleted { get; set; }
    }

    // AppParamsDelta tracks a changed AppParams, and whether it was deleted
    public partial class AppParamsDelta
    {
        public AppParams Params { get; set; }
        public bool Deleted { get; set; }
    }

    // AppResourceRecord represents AppParams and AppLocalState in deltas
    public partial class AppResourceRecord
    {
        public ulong Aidx { get; set; }
        public Address Addr { get; set; }
        public AppParamsDelta Params { get; set; }
        public AppLocalStateDelta State { get; set; }
    }

    // AssetHolding describes an asset held by an account.
    public partial class AssetHolding
    {

        [Newtonsoft.Json.JsonProperty("a", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ulong Amount { get; set; }

        [Newtonsoft.Json.JsonProperty("f", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Frozen { get; set; }
    }

    // AssetHoldingDelta records a changed AssetHolding, and whether it was deleted
    public partial class AssetHoldingDelta
    {
        public AssetHolding Holding { get; set; }
        public bool Deleted { get; set; }
    }

    // AssetParamsDelta tracks a changed AssetParams, and whether it was deleted
    public partial class AssetParamsDelta
    {
        public AssetParams Params { get; set; }
        public bool Deleted { get; set; }
    }

    // AssetResourceRecord represents AssetParams and AssetHolding in deltas
    public partial class AssetResourceRecord
    {
        public ulong Aidx { get; set; }
        public Address Addr { get; set; }
        public AssetParamsDelta Params { get; set; }
        public AssetHoldingDelta Holding { get; set; }
    }

    // AccountAsset is used as a Dictionary key.
    public partial class AccountAsset
    {
        public Address Address { get; set; }
        public ulong Asset { get; set; }
    }

    // AccountApp is used as a Dictionary key.
    public partial class AccountApp
    {
        public Address Address { get; set; }
        public ulong App { get; set; }
    }







    // AccountData provides users of the Balances interface per-account data (like basics.AccountData)
    // but without any Dictionarys containing AppParams, AppLocalState, AssetHolding, or AssetParams. This
    // ensures that transaction evaluation must retrieve and mutate account, asset, and application data
    // separately, to better support on-disk and in-memory schemas that do not store them together.
    public partial class AccountData
    {
        public byte Status { get; set; }
        public ulong MicroAlgos { get; set; }
        public ulong RewardsBase { get; set; }
        public ulong RewardedMicroAlgos { get; set; }
        public Address AuthAddr { get; set; }
        public StateSchema TotalAppSchema { get; set; } // Totals across created globals, and opted in locals.
        public uint TotalExtraAppPages { get; set; } // Total number of extra pages across all created apps
        public ulong TotalAppParams { get; set; } // Total number of apps this account has created
        public ulong TotalAppLocalStates { get; set; } // Total number of apps this account is opted into.
        public ulong TotalAssetParams { get; set; } // Total number of assets created by this account
        public ulong TotalAssets { get; set; } // Total of asset creations and optins (i.e. number of holdings)
        public ulong TotalBoxes { get; set; } // Total number of boxes associated to this account
        public ulong TotalBoxBytes { get; set; } // Total bytes for this account's boxes. keys _and_ values count
        public byte[] VoteID { get; set; }
        public byte[] SelectionID { get; set; }
        public byte[] StateProofID { get; set; }
        public ulong VoteFirstValid { get; set; }
        public ulong VoteLastValid { get; set; }
        public ulong VoteKeyDilution { get; set; }

    }

    // BalanceRecord is similar to basics.BalanceRecord but with decoupled base and voting data
    public partial class BalanceRecord : AccountData
    {
        public Address Addr { get; set; }

    }

    // AccountDeltas stores ordered accounts and allows fast lookup by address
    // One key design aspect here was to ensure that we're able to access the written
    // deltas in a deterministic order, while maintaining O(1) lookup. In order to
    // do that, each of the arrays here is constructed as a pair of (slice, Dictionary).
    // The Dictionary would point the address/address+creatable id onto the index of the
    // element within the slice.
    // If adding fields make sure to add them to the .reset() method to avoid dirty state
    public partial class AccountDeltas
    {
        public BalanceRecord[] Accts { get; set; } // cache for addr to deltas index resolution
        public Dictionary<Address, nint> acctsCache { get; set; } // AppResources deltas. If app params or local state is deleted, there is a nil value in AppResources.Params or AppResources.State and Deleted flag set
        public AppResourceRecord[] AppResources { get; set; } // caches for {addr, app id} to app params delta resolution
                                                              // not preallocated - use UpsertAppResource instead of inserting directly
        public Dictionary<AccountApp, nint> appResourcesCache { get; set; }
        public AssetResourceRecord[] AssetResources { get; set; } // not preallocated - use UpsertAssertResource instead of inserting directly
        public Dictionary<AccountAsset, nint> assetResourcesCache { get; set; }
    }

    // A KvValueDelta shows how the Data associated with a key in the kvstore has
    // changed.  However, OldData is elided during evaluation, and only filled in at
    // the conclusion of a block during the called to roundCowState.deltas()
    public partial class KvValueDelta
    {
        public byte[] Data { get; set; } // OldData stores the previous vlaue (nil == didn't exist)
        public byte[] OldData { get; set; }
    }

    // IncludedTransactions defines the transactions included in a block, their index and last valid round.
    public partial class IncludedTransactions
    {
        public ulong LastValid { get; set; }
        public ulong Intra { get; set; } // the index of the transaction in the block
    }



    // A Txlease is a transaction (sender, lease) pair which uniquely specifies a
    // transaction lease.
    public partial class Txlease
    {
        public Address Sender { get; set; }
        public byte[] Lease { get; set; }
    }

    // CreatableIndex represents either an AssetIndex or AppIndex, which come from
    // the same namespace of indices as each other (both assets and apps are
    // "creatables")




    // ModifiedCreatable defines the changes to a single single creatable state
    public partial class ModifiedCreatable
    {
        public ulong Ctype { get; set; } // Created if true, deleted if false
        public bool Created { get; set; } // creator of the app/asset
        public Address Creator { get; set; } // Keeps track of how many times this app/asset appears in
                                             // accountUpdates.creatableDeltas
        public nint Ndeltas { get; set; }
    }

    // AlgoCount represents a total of algos of a certain class
    // of accounts (split up by their Status value).
    public partial class AlgoCount
    {

        [Newtonsoft.Json.JsonProperty("mon", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ulong Money { get; set; } // Total number of whole reward units in accounts.

        [Newtonsoft.Json.JsonProperty("rwd", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ulong RewardUnits { get; set; }
    }

    // AccountTotals represents the totals of algos in the system
    // grouped by different account status values.
    public partial class AccountTotals
    {

        [Newtonsoft.Json.JsonProperty("online", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public AlgoCount Online { get; set; }

        [Newtonsoft.Json.JsonProperty("offline", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public AlgoCount Offline { get; set; }

        [Newtonsoft.Json.JsonProperty("notpart", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public AlgoCount NotParticipating { get; set; } // Total number of algos received per reward unit since genesis

        [Newtonsoft.Json.JsonProperty("rwdlvl", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ulong RewardsLevel { get; set; }
    }

    // LedgerStateDelta describes the delta between a given round to the previous round
    // If adding a new field not explicitly allocated by PopulateStateDelta, make sure to reset
    // it in .ReuseStateDelta to avoid dirty memory errors.
    // If adding fields make sure to add them to the .Reset() method to avoid dirty state
    public partial class LedgerStateDelta
    {
        public AccountDeltas Accts { get; set; } // modified kv pairs (nil == delete)
                                                 // not preallocated use .AddKvMod to insert instead of direct assignment
        public Dictionary<string, KvValueDelta> KvMods { get; set; } // new Txids for the txtail and TxnCounter, Dictionaryped to txn.LastValid
        public Dictionary<string, IncludedTransactions> Txids { get; set; } // new txleases for the txtail Dictionaryped to expiration
                                                                            // not pre-allocated so use .AddTxLease to insert instead of direct assignment
        public Dictionary<Txlease, ulong> Txleases { get; set; } // new creatables creator lookup table
                                                                 // not pre-allocated so use .AddCreatable to insert instead of direct assignment
        public Dictionary<ulong, ModifiedCreatable> Creatables { get; set; } // new block header { get; set; } read-only
        public Block Hdr { get; set; } // StateProofNext represents modification on StateProofNextRound field in the block header. If the block contains
                                       // a valid state proof transaction, this field will contain the next round for state proof.
                                       // otherwise it will be set to 0.
        public ulong StateProofNext { get; set; } // previous block timestamp
        public long PrevTimestamp { get; set; } // initial hint for allocating data structures for StateDelta
        public nint initialHint { get; set; } // The account totals reflecting the changes in this StateDelta object.
        public AccountTotals Totals { get; set; }
    }
}


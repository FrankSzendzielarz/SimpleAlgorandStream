using Algorand;
using Algorand.Algod.Model;
using Algorand.Algod.Model.Transactions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimpleAlgorandStream.Model
{


    // TealValue contains type information and a value, representing a value in a
    // TEAL program
    public partial class TealValue
    {
        [Description("codec:\"tt\"")]
        public ulong Type;
        [Description("codec:\"tb\"")]
        public string Bytes;
        [Description("codec:\"ui\"")]
        public ulong Uint;
    }

    // TealKeyValue represents a key/value store for use in an application's
    // LocalState or GlobalState
    //
    //msgp:allocbound TealKeyValue EncodedMaxKeyValueEntries
    public partial class TealKeyValue
    { // : Dictionary<@string, TealValue>
    }

    // StateSchemas is a thin wrapper around the LocalStateSchema and the
    // GlobalStateSchema, since they are often needed together
    public partial class StateSchemas
    {
        [Description("codec:\"lsch\"")]
        public StateSchema LocalStateSchema;
        [Description("codec:\"gsch\"")]
        public StateSchema GlobalStateSchema;
    }

    // AppParams stores the global information associated with an application
    public partial class AppParams
    {
        [Description("codec:\"approv,allocbound=config.MaxAvailableAppProgramLen\"")]
        public byte[] ApprovalProgram;
        [Description("codec:\"clearp,allocbound=config.MaxAvailableAppProgramLen\"")]
        public byte[] ClearStateProgram;
        [Description("codec:\"gs\"")]
        public TealKeyValue GlobalState;
        public StateSchemas StateSchemas;
        [Description("codec:\"epp\"")]
        public uint ExtraProgramPages;
    }

    // AppLocalState stores the LocalState associated with an application. It also
    // stores a cached copy of the application's LocalStateSchema so that
    // MinBalance requirements may be computed 1. without looking up the
    // AppParams and 2. even if the application has been deleted
    public partial class AppLocalState
    {
        [Description("codec:\"hsch\"")]
        public StateSchema Schema;
        [Description("codec:\"tkv\"")]
        public TealKeyValue KeyValue;
    }

    // AppLocalStateDelta tracks a changed AppLocalState, and whether it was deleted
    public partial class AppLocalStateDelta
    {
        public AppLocalState LocalState;
        public bool Deleted;
    }

    // AppParamsDelta tracks a changed AppParams, and whether it was deleted
    public partial class AppParamsDelta
    {
        public AppParams Params;
        public bool Deleted;
    }

    // AppResourceRecord represents AppParams and AppLocalState in deltas
    public partial class AppResourceRecord
    {
        public ulong Aidx;
        public Address Addr;
        public AppParamsDelta Params;
        public AppLocalStateDelta State;
    }

    // AssetHolding describes an asset held by an account.
    public partial class AssetHolding
    {
        [Description("codec:\"a\"")]
        public ulong Amount;
        [Description("codec:\"f\"")]
        public bool Frozen;
    }

    // AssetHoldingDelta records a changed AssetHolding, and whether it was deleted
    public partial class AssetHoldingDelta
    {
        public AssetHolding Holding;
        public bool Deleted;
    }

    // AssetParamsDelta tracks a changed AssetParams, and whether it was deleted
    public partial class AssetParamsDelta
    {
        public AssetParams Params;
        public bool Deleted;
    }

    // AssetResourceRecord represents AssetParams and AssetHolding in deltas
    public partial class AssetResourceRecord
    {
        public ulong Aidx;
        public Address Addr;
        public AssetParamsDelta Params;
        public AssetHoldingDelta Holding;
    }

    // AccountAsset is used as a Dictionary key.
    public partial class AccountAsset
    {
        public Address Address;
        public ulong Asset;
    }

    // AccountApp is used as a Dictionary key.
    public partial class AccountApp
    {
        public Address Address;
        public ulong App;
    }

  

 

    // VotingData holds participation information
    public partial class VotingData
    {
        public byte[] VoteID;
        public byte[] SelectionID;
        public byte[] StateProofID;
        public ulong VoteFirstValid;
        public ulong VoteLastValid;
        public ulong VoteKeyDilution;
    }

    // AccountBaseData contains base account info like balance, status and total number of resources
    public partial class AccountBaseData
    {
        public byte Status;
        public ulong MicroAlgos;
        public ulong RewardsBase;
        public ulong RewardedMicroAlgos;
        public Address AuthAddr;
        public StateSchema TotalAppSchema; // Totals across created globals, and opted in locals.
        public uint TotalExtraAppPages; // Total number of extra pages across all created apps
        public ulong TotalAppParams; // Total number of apps this account has created
        public ulong TotalAppLocalStates; // Total number of apps this account is opted into.
        public ulong TotalAssetParams; // Total number of assets created by this account
        public ulong TotalAssets; // Total of asset creations and optins (i.e. number of holdings)
        public ulong TotalBoxes; // Total number of boxes associated to this account
        public ulong TotalBoxBytes; // Total bytes for this account's boxes. keys _and_ values count
    }

    // AccountData provides users of the Balances interface per-account data (like basics.AccountData)
    // but without any Dictionarys containing AppParams, AppLocalState, AssetHolding, or AssetParams. This
    // ensures that transaction evaluation must retrieve and mutate account, asset, and application data
    // separately, to better support on-disk and in-memory schemas that do not store them together.
    public partial class AccountData
    {
        public AccountBaseData AccountBaseData;
        public VotingData VotingData;
    }

    // BalanceRecord is similar to basics.BalanceRecord but with decoupled base and voting data
    public partial class BalanceRecord
    {
        public Address Addr;
        public AccountData AccountData;
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
        public BalanceRecord[] Accts; // cache for addr to deltas index resolution
        public Dictionary<Address, nint> acctsCache; // AppResources deltas. If app params or local state is deleted, there is a nil value in AppResources.Params or AppResources.State and Deleted flag set
        public AppResourceRecord[] AppResources; // caches for {addr, app id} to app params delta resolution
                                                      // not preallocated - use UpsertAppResource instead of inserting directly
        public Dictionary<AccountApp, nint> appResourcesCache;
        public AssetResourceRecord[] AssetResources; // not preallocated - use UpsertAssertResource instead of inserting directly
        public Dictionary<AccountAsset, nint> assetResourcesCache;
    }

    // A KvValueDelta shows how the Data associated with a key in the kvstore has
    // changed.  However, OldData is elided during evaluation, and only filled in at
    // the conclusion of a block during the called to roundCowState.deltas()
    public partial class KvValueDelta
    {
        public byte[] Data; // OldData stores the previous vlaue (nil == didn't exist)
        public byte[] OldData;
    }

    // IncludedTransactions defines the transactions included in a block, their index and last valid round.
    public partial class IncludedTransactions
    {
        public ulong LastValid;
        public ulong Intra; // the index of the transaction in the block
    }

    // Txid is a hash used to uniquely identify individual transactions
    public partial class Txid
    { // : Digest
    }

    // A Txlease is a transaction (sender, lease) pair which uniquely specifies a
    // transaction lease.
    public partial class Txlease
    {
        public Address Sender;
        public byte[] Lease;
    }

    // CreatableIndex represents either an AssetIndex or AppIndex, which come from
    // the same namespace of indices as each other (both assets and apps are
    // "creatables")
    public partial class CreatableIndex
    { // : ulong
    }

    // CreatableType is an enum representing whether or not a given creatable is an
    // application or an asset
    public partial class CreatableType
    { // : ulong
    }

    // ModifiedCreatable defines the changes to a single single creatable state
    public partial class ModifiedCreatable
    {
        public CreatableType Ctype; // Created if true, deleted if false
        public bool Created; // creator of the app/asset
        public Address Creator; // Keeps track of how many times this app/asset appears in
                                // accountUpdates.creatableDeltas
        public nint Ndeltas;
    }

    // AlgoCount represents a total of algos of a certain class
    // of accounts (split up by their Status value).
    public partial class AlgoCount
    {
        [Description("codec:\"mon\"")]
        public ulong Money; // Total number of whole reward units in accounts.
        [Description("codec:\"rwd\"")]
        public ulong RewardUnits;
    }

    // AccountTotals represents the totals of algos in the system
    // grouped by different account status values.
    public partial class AccountTotals
    {
        [Description("codec:\"online\"")]
        public AlgoCount Online;
        [Description("codec:\"offline\"")]
        public AlgoCount Offline;
        [Description("codec:\"notpart\"")]
        public AlgoCount NotParticipating; // Total number of algos received per reward unit since genesis
        [Description("codec:\"rwdlvl\"")]
        public ulong RewardsLevel;
    }

    // LedgerStateDelta describes the delta between a given round to the previous round
    // If adding a new field not explicitly allocated by PopulateStateDelta, make sure to reset
    // it in .ReuseStateDelta to avoid dirty memory errors.
    // If adding fields make sure to add them to the .Reset() method to avoid dirty state
    public partial class LedgerStateDelta
    {
        public AccountDeltas Accts; // modified kv pairs (nil == delete)
                                    // not preallocated use .AddKvMod to insert instead of direct assignment
        public Dictionary<string, KvValueDelta> KvMods; // new Txids for the txtail and TxnCounter, Dictionaryped to txn.LastValid
        public Dictionary<Txid, IncludedTransactions> Txids; // new txleases for the txtail Dictionaryped to expiration
                                                             // not pre-allocated so use .AddTxLease to insert instead of direct assignment
        public Dictionary<Txlease, ulong> Txleases; // new creatables creator lookup table
                                                    // not pre-allocated so use .AddCreatable to insert instead of direct assignment
        public Dictionary<CreatableIndex, ModifiedCreatable> Creatables; // new block header; read-only
        public Block Hdr; // StateProofNext represents modification on StateProofNextRound field in the block header. If the block contains
                                     // a valid state proof transaction, this field will contain the next round for state proof.
                                     // otherwise it will be set to 0.
        public ulong StateProofNext; // previous block timestamp
        public long PrevTimestamp; // initial hint for allocating data structures for StateDelta
        public nint initialHint; // The account totals reflecting the changes in this StateDelta object.
        public AccountTotals Totals;
    }
}


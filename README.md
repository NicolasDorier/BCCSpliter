# BCCSpliter
A command line utility for splitting your BTC from your BCC.

## Motivation
The 1st august 2017, a minority of the community is creating an altcoin named BCC (Bitcoin Cash) whose initial distribution is holding of BTC at the time of the hard fork.

This tool allows you to connect to your Bitcoin Core wallet, select coins you want to split, create a transaction only valid on BCC and broadcast it.

BCC includes an opt-in replay protection: a new flag for signature `SIGHASH_FORKID` has been created in BCC. The signature scheme for this SIGHASH, is similar to segwit signature scheme.

This mean that a valid `SIGHASH_FORKID` signature on BCC will not be valid on BTC chain. Making it easy to split your UTXO.

### Privacy disclaimer

If you are dumping coins from your bitcoin core, no external resources than your node is used.
For other use case (`dumpprivkey` and `dumphd`), a block explorer is used and leak your addresses. (QBitNinja)

## Requirements

As a user, you will need:

1. [NET Core SDK 1.0.4](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.4-sdk-download.md) (see below)
2. At least [Bitcoin Core 0.13.1](https://bitcoin.org/bin/bitcoin-core-0.13.1/) fully sync, rpc enabled.

First add the repository for the SDK by following these [instructions](https://www.microsoft.com/net/core#linuxubuntu)

Afterwards you can install the runtime by running
```
sudo apt-get install dotnet-dev-1.0.4
```
You can known more about install on your system on [this link](https://www.microsoft.com/net/core).
Using Bitcoin Core with later version should work as well.

As a developer, you need additionally one of those:

1. [Visual studio 2017](https://www.visualstudio.com/downloads/) (Windows only)
2. [Visual studio code](https://code.visualstudio.com/) with [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) (Cross plateform)

## How to build?

``` 
sudo apt-get install git
```

Given the requirements are installed in your system and BitcoinD is running:

Setup your repository:
```
git clone https://github.com/NicolasDorier/BCCSpliter
cd BCCSpliter
git submodule init
git submodule update
dotnet restore
```

## How to run?
For running, from inside the repository folder:
```
cd BCCSpliter
dotnet run
```
Be sure you are in the project folder inside the repository (BCCSpliter/BCCSpliter) when running `dotnet run`.

If you want to run on other network (regtest or testnet):

```
dotnet run -testnet
```

```
dotnet run -regtest
```

## How to dump coin in my Bitcoin Core Wallet?

The process is the following:

1. Select the UTXOs your want to spend.
2. Use the command `dump <BitcoinAddress on BCC>` (This will create and broadcast a valid on BCC only, and lock the UTXOs you used in Bitcoin Core wallet)

Typical flow:

```
list
# Find interesting UTXO you want to dump
select 72c5ddde9377d5332cd4a29412326272c2678c4020520508328bba7a351d7263-0
select 489dc8f0925a99fe772f0d5cd5f05cda77bf29449ec3ee266575add6312d6e04-2
# Check the status what the next dump will be
list
# Then dump
dump mpxsXnCn1ScUVZmgEDgkjMZMV3NAAVzSdN
```

The selected UTXO are now safe to be spent on BTC chain.

### How to dump my paper wallet key?

Use `dumprivkey BASE58PRIVKEY DESTINATION`

Example:
```
dumpprivkey cRkaHCt5FuD7x4SR5hAP5yWA5JnfhAYrVREfgUG2YdQ9bSnBjdgb miUraop8F4R4kVDczUfucF3itB4t6wtB5q 
```

A BlockExplorer (QBitNinja) is used for scanning your funds.

### How to dump my HD key?

Use `dumphd HDKEY DESTINATION`

Example:
```
dumphd xpriv..... miUraop8F4R4kVDczUfucF3itB4t6wtB5q 
```

A BlockExplorer (QBitNinja) is used for scanning your funds.

If it does not find your coin, please open an issue. There is several way of deriving addresses from HD, and I lazily support them.

## How to configure?
If you are not using standard install for bitcoind, you will have to change the configuration file:
In Windows it is located on 

```
C:\Users\<user>\AppData\Roaming\BCCSpliter\<network>\spliter.config
```

On linux or mac:
```
~/.bccspliter/<network>/spliter.config
```

The configuration file allows to configure the connection to your Bitcoin RPC, and to choose a specific Bitcoin Cash node for broadcasting your transaciton.
By default, the configuration file is using cookie authentication in default bitcoin core path, and connect to a random Bitcoin Cash node.

## My transaction does not pass

Please, wait for the split to happen before using this tool on Mainnet. 

If that is the case, the problem is that your Bitcoin Core has no idea which coin can be splitted, and it is possible you splitted an UTXO which does not exist on the BCC chain.

If that is the case, then your transaction will be rejected, and your machine banned by BCC nodes.

There is sadly no block explorer for BCC to check which UTXO can be splitted at this time, so this is a trial and error process.

## License
This program is under [MIT License](https://github.com/NicolasDorier/BCCSpliter/blob/master/LICENSE), use at your own risk.

If you are unsure about what this program is doing, then do not use it. This program is meant for people understanding the situation with BCC and BTC.

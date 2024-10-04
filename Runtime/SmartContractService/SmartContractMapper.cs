using SCS;

public static class SmartContractMapper
{
    public static SmartContract Map(this SmartContractDTO contract)
    {
        return new SmartContract
        {
            ABI = contract.ABI,
            Address = contract.Address,
            Type = contract.Type switch
            {
                "ERC20Token" => SmartContractType.ERC20Token,
                "SecurityDeposit" => SmartContractType.SecurityDeposit,
                "OnChainDuel" => SmartContractType.OnChainDuel,
                _ => throw new InvalidSmartContractTypeException("Unrecognized SmartContract type."),
            }
        };
    }
}

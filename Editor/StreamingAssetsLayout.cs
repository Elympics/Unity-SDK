namespace Elympics.Editor
{
    /// <summary>
    /// Shape of the local directory passed to a StreamingAssets content upload. It decides which files are
    /// collected, what the relative paths sent to the backend look like, and how much the directory is validated.
    /// </summary>
    public enum StreamingAssetsLayout
    {
        /// <summary>
        /// The default. One directory per variant, each an Addressables content build - so every file sits under
        /// <c>{variant}/aa/...</c> and uploads under that path. An <c>aa</c> directory at any other depth is an
        /// error. Files sitting directly at the path are uploaded too, with a warning. Also uploads a generated
        /// <c>variants.meta.json</c> listing the variants.
        /// </summary>
        AddressableVariants,

        /// <summary>
        /// Every file below the path, whatever the structure, with no structural validation.
        /// </summary>
        UnstructuredAssets,
    }
}

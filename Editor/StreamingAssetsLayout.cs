namespace Elympics.Editor
{
    /// <summary>Shape of the local directory passed to a StreamingAssets content upload.</summary>
    public enum StreamingAssetsLayout
    {
        /// <summary>
        /// The default. One Addressable build directory per variant.
        /// Every file sits under <c>{variant}/aa/...</c> and uploads under that path.
        /// Nested <c>aa</c> directories are disallowed.
        /// Files directly at the root path are uploaded too, but result in a warning.
        /// </summary>
        /// <remarks><c>variants.meta.json</c> listing the variants is generated and uploaded to bucket at the root path.</remarks>
        AddressableVariants,

        /// <summary>
        /// No structural validation.
        /// Every file is uploaded as is.
        /// </summary>
        UnstructuredAssets,
    }
}

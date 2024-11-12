local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local moreLockBlocks = require("mods").requireFromPlugin("libraries.more_lock_blocks")

local dreamLockBlock = {}

dreamLockBlock.name = "MoreLockBlocks/DreamLockBlock"
dreamLockBlock.depth = function(room, entity) return entity.below and 5000 or -11000 end
dreamLockBlock.placements = {
    {
        name = "dreamLockBlock",
        data = {
            spritePath = "",
            unlock_sfx = "",
            stepMusicProgress = false,
            useVanillaKeys = true,
            dzhakeHelperKeySettings = "",
            below = false,
            ignoreInventory = true,
        }
    }
}
dreamLockBlock.fieldInformation = {
    dzhakeHelperKeySettings = {
        fieldType = "string",
        validator = moreLockBlocks.dzhakeHelperKeySettingsValidator
    }
}

local defaultLockTexture = "objects/MoreLockBlocks/generic/lock00"
local blockColor = { 0.0, 0.0, 0.0, 1.0 }
local blockBorderColor = { 1.0, 1.0, 1.0, 1.0 }

function dreamLockBlock.sprite(room, entity)
    local rectangle = drawableRectangle.fromRectangle(
        "bordered",
        entity.x, entity.y, 32, 32,
        blockColor,
        blockBorderColor
    )

    local lockTexture = moreLockBlocks.isempty(entity.spritePath) and defaultLockTexture or (entity.spritePath .. "00")
    local lockSprite = drawableSprite.fromTexture(lockTexture, entity)
    lockSprite:addPosition(16, 16)

    return { rectangle, lockSprite }
end

function dreamLockBlock.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, 32, 32)
end

return dreamLockBlock

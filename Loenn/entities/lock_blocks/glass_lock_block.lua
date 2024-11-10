local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local moreLockBlocks = require("mods").requireFromPlugin("libraries.more_lock_blocks")

local glassLockBlock = {}

glassLockBlock.name = "MoreLockBlocks/GlassLockBlock"
glassLockBlock.depth = function(room, entity) return entity.behindFgTiles and -9995 or -10000 end
glassLockBlock.placements = {
    {
        name = "glassLockBlock",
        data = {
            spritePath = "",
            unlock_sfx = "",
            stepMusicProgress = false,
            behindFgTiles = false,
            useVanillaKeys = true,
            dzhakeHelperKeySettings = "",
        }
    }
}
glassLockBlock.fieldInformation = {
    dzhakeHelperKeySettings = {
        fieldType = "string",
        validator = moreLockBlocks.dzhakeHelperKeySettingsValidator
    }
}

local defaultLockTexture = "objects/MoreLockBlocks/generic/lock00"
local defaultBlockColor = { 13 / 255, 46 / 255, 137 / 255 }
local defaultBlockBorderColor = { 1.0, 1.0, 1.0, 1.0 }
local fallbackBlockColor = { 1.0, 1.0, 1.0, 0.6 }
local fallbackBlockBorderColor = { 1.0, 1.0, 1.0, 0.8 }

function glassLockBlock.sprite(room, entity)
    local controllerBlockColor, controllerBlockBorderColor
    for _, e in ipairs(room.entities) do
        if e._name == "MoreLockBlocks/GlassLockBlockController" then
            controllerBlockColor = utils.getColor(e.bgColor)
            controllerBlockBorderColor = utils.getColor(e.lineColor)
            break
        end
    end

    local rectangle = drawableRectangle.fromRectangle(
        "bordered",
        entity.x, entity.y, 32, 32,
        controllerBlockColor or defaultBlockColor or fallbackBlockColor,
        controllerBlockBorderColor or defaultBlockBorderColor or fallbackBlockBorderColor
    )

    local lockTexture = moreLockBlocks.isempty(entity.spritePath) and defaultLockTexture or (entity.spritePath .. "00")
    local lockSprite = drawableSprite.fromTexture(lockTexture, entity)
    lockSprite:addPosition(16, 16)

    return { rectangle, lockSprite }
end

function glassLockBlock.selection(room, entity)
    return utils.rectangle(entity.x, entity.y, 32, 32)
end

return glassLockBlock

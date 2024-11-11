local moreLockBlocks = {}

function moreLockBlocks.isempty(s)
    return s == nil or s == ""
end

function moreLockBlocks.dzhakeHelperKeySettingsValidator(settings)
    return settings == "" or settings == "*" or (tonumber(settings) ~= nil and not string.find(settings, ".", 1, true))
end

return moreLockBlocks
